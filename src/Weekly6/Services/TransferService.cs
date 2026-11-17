using Microsoft.EntityFrameworkCore;
using Weekly6.Data;
using Weekly6.DTOs;
using Weekly6.Models;
using Weekly6.Repositories;

namespace Weekly6.Services;

public sealed class TransferService(
  IWalletRepository walletRepository,
  AppDbContext db) : ITransferService
{
  private readonly IWalletRepository _walletRepository = walletRepository;
  private readonly AppDbContext _db = db;

  public async Task<TransactionResponse?> TransferAsync(
    TransferRequest req,
    string? correlationId,
    CancellationToken cancellationToken)
  {
    // Validasi
    if (req.SenderWalletId == req.ReceiverWalletId)
    {
      await LogRejectedTransferAsync(
        actorUserId: null,
        entityId: req.SenderWalletId.ToString(),
        description: "Tidak dapat melakukan transfer ke wallet sendiri.",
        correlationId: correlationId,
        cancellationToken: cancellationToken);

      throw new InvalidOperationException("Tidak bisa melakukan transfer ke akun sendiri.");
    }

    if (req.Amount <= 0)
    {
      await LogRejectedTransferAsync(
        actorUserId: null,
        entityId: req.SenderWalletId.ToString(),
        description: "Nominal transfer harus lebih dari 0.",
        correlationId: correlationId,
        cancellationToken: cancellationToken);

      throw new ArgumentOutOfRangeException(nameof(req.Amount), "Nominal transfer harus lebih dari 0.");
    }

    var senderWallet = await _walletRepository.GetByIdAsync(req.SenderWalletId, cancellationToken);
    var receiverWallet = await _walletRepository.GetByIdAsync(req.ReceiverWalletId, cancellationToken);

    if (senderWallet is null || receiverWallet is null)
    {
      await LogRejectedTransferAsync(
        actorUserId: senderWallet?.UserId,
        entityId: req.SenderWalletId.ToString(),
        description: "Wallet pengirim atau penerima tidak ditemukan.",
        correlationId: correlationId,
        cancellationToken: cancellationToken);

      return null;
    }

    if (senderWallet.Status != "ACTIVE" || receiverWallet.Status != "ACTIVE")
    {
      var reason = $"Status wallet tidak valid (Sender: {senderWallet.Status}, Receiver: {receiverWallet.Status}).";

      await RecordFailedTransferAsync(
        senderWallet: senderWallet,
        receiverWallet: receiverWallet,
        req: req,
        reason: reason,
        action: "TRANSFER_REJECTED",
        outcome: "REJECTED",
        correlationId: correlationId,
        cancellationToken: cancellationToken);

      throw new InvalidOperationException("Wallet pengirim dan penerima harus berstatus ACTIVE.");
    }

    if (senderWallet.Balance < req.Amount)
    {
      var reason = "Saldo pengirim tidak mencukupi untuk melakukan transfer.";

      await RecordFailedTransferAsync(
        senderWallet: senderWallet,
        receiverWallet: receiverWallet,
        req: req,
        reason: reason,
        action: "TRANSFER_REJECTED",
        outcome: "REJECTED",
        correlationId: correlationId,
        cancellationToken: cancellationToken);

      throw new InvalidOperationException(reason);
    }

    // Begin Transaction
    await using var dbTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);

    try
    {
      var senderBalanceBefore = senderWallet.Balance;
      var receiverBalanceBefore = receiverWallet.Balance;

      senderWallet.DecreaseBalance(req.Amount);
      receiverWallet.IncreaseBalance(req.Amount);

      var senderBalanceAfter = senderWallet.Balance;
      var receiverBalanceAfter = receiverWallet.Balance;

      var now = DateTime.UtcNow;
      var referenceNo = GenerateReferenceNo(now);

      var transaction = new Transaction(
        referenceNo: referenceNo,
        senderWalletId: senderWallet.Id,
        receiverWalletId: receiverWallet.Id,
        type: "TRANSFER",
        amount: req.Amount,
        status: "SUCCESS",
        description: string.IsNullOrWhiteSpace(req.Description)
          ? $"Transfer ke {receiverWallet.WalletNumber}"
          : req.Description.Trim(),
        completedAtUtc: now);

      await _db.Transactions.AddAsync(transaction, cancellationToken);
      await _db.SaveChangesAsync(cancellationToken);

      var outMutation = new WalletMutation(
        transactionId: transaction.Id,
        walletId: senderWallet.Id,
        direction: "OUT",
        amount: req.Amount,
        balanceBefore: senderBalanceBefore,
        balanceAfter: senderBalanceAfter);

      var inMutation = new WalletMutation(
        transactionId: transaction.Id,
        walletId: receiverWallet.Id,
        direction: "IN",
        amount: req.Amount,
        balanceBefore: receiverBalanceBefore,
        balanceAfter: receiverBalanceAfter);

      await _db.WalletMutations.AddRangeAsync([outMutation, inMutation], cancellationToken);

      var auditLog = new AuditLog(
        actorUserId: senderWallet.UserId,
        transactionId: transaction.Id,
        action: "TRANSFER_CREATED",
        entityType: "Transaction",
        entityId: transaction.Id.ToString(),
        outcome: "SUCCESS",
        description: $"Transfer referensi {referenceNo} sebesar {req.Amount:N0} dari {senderWallet.WalletNumber} ke {receiverWallet.WalletNumber} berhasil.",
        correlationId: correlationId);

      await _db.AuditLogs.AddAsync(auditLog, cancellationToken);

      await _db.SaveChangesAsync(cancellationToken);
      // Commit
      await dbTransaction.CommitAsync(cancellationToken);

      return TransactionResponse.FromModel(transaction);
    }
    catch (DbUpdateConcurrencyException)
    {
      // Rollback
      await dbTransaction.RollbackAsync(cancellationToken);
      _db.ChangeTracker.Clear();

      await RecordFailedTransferAsync(
        senderWallet: senderWallet,
        receiverWallet: receiverWallet,
        req: req,
        reason: "Konflik pembaruan saldo secara bersamaan (Concurrency Conflict).",
        action: "TRANSFER_FAILED",
        outcome: "FAILED",
        correlationId: correlationId,
        cancellationToken: cancellationToken);

      throw;
    }
    catch
    {
      await dbTransaction.RollbackAsync(cancellationToken);
      _db.ChangeTracker.Clear();
      throw;
    }
  }

  // ========================= Method Helper
  private static string GenerateReferenceNo(DateTime utcNow) =>
    $"TRF-{utcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

  private async Task RecordFailedTransferAsync(
    Wallet senderWallet,
    Wallet receiverWallet,
    TransferRequest req,
    string reason,
    string action,
    string outcome,
    string? correlationId,
    CancellationToken cancellationToken)
  {
    var now = DateTime.UtcNow;
    var referenceNo = GenerateReferenceNo(now);

    var failedTransaction = new Transaction(
      referenceNo: referenceNo,
      senderWalletId: senderWallet.Id,
      receiverWalletId: receiverWallet.Id,
      type: "TRANSFER",
      amount: req.Amount,
      status: "FAILED",
      description: string.IsNullOrWhiteSpace(req.Description)
        ? $"Gagal transfer ke {receiverWallet.WalletNumber}: {reason}"
        : $"{req.Description.Trim()} (Gagal: {reason})",
      completedAtUtc: now);

    await _db.Transactions.AddAsync(failedTransaction, cancellationToken);
    await _db.SaveChangesAsync(cancellationToken);

    var auditLog = new AuditLog(
      actorUserId: senderWallet.UserId,
      transactionId: failedTransaction.Id,
      action: action,
      entityType: "Transaction",
      entityId: failedTransaction.Id.ToString(),
      outcome: outcome,
      description: $"Transfer referensi {referenceNo} gagal: {reason}",
      correlationId: correlationId);

    await _db.AuditLogs.AddAsync(auditLog, cancellationToken);
    await _db.SaveChangesAsync(cancellationToken);
  }

  private async Task LogRejectedTransferAsync(
    long? actorUserId,
    string? entityId,
    string description,
    string? correlationId,
    CancellationToken cancellationToken)
  {
    var rejectedLog = new AuditLog(
      actorUserId: actorUserId,
      transactionId: null,
      action: "TRANSFER_REJECTED",
      entityType: "Wallet",
      entityId: entityId,
      outcome: "REJECTED",
      description: description,
      correlationId: correlationId);

    await _db.AuditLogs.AddAsync(rejectedLog, cancellationToken);
    await _db.SaveChangesAsync(cancellationToken);
  }
}