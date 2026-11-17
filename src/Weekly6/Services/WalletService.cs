using Microsoft.EntityFrameworkCore;
using Weekly6.Data;
using Weekly6.DTOs;
using Weekly6.Models;
using Weekly6.Repositories;

namespace Weekly6.Services;

public sealed class WalletService(AppDbContext db, IWalletRepository walletRepository, IPaymentMethodRepository paymentMethodRepository) : IWalletService
{
  private readonly AppDbContext _db = db;
  private readonly IWalletRepository _walletRepository = walletRepository;
  private readonly IPaymentMethodRepository _paymentMethodRepository = paymentMethodRepository;

  public async Task<WalletResponse?> GetByUserIdAsync(long userId, CancellationToken cancellationToken)
  {
    if (userId <= 0)
    {
      return null;
    }

    var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);
    return wallet is null ? null : WalletResponse.FromModel(wallet);
  }

  public async Task<Wallet?> GetByIdAsync(long id, CancellationToken cancellationToken)
  {
    return await _walletRepository.GetReadOnlyByIdAsync(id, cancellationToken);
  }

  public async Task<Wallet?> GetByWalletNumberAsync(string walletNumber, CancellationToken cancellationToken)
  {
    return await _walletRepository.GetByWalletNumberAsync(walletNumber, cancellationToken);
  }

  public async Task<TopUpResponse?> RequestTopUpAsync(
    long userId,
    TopUpRequestDto req,
    string? correlationId,
    CancellationToken cancellationToken
  )
  {
    var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken);

    if (wallet is null || wallet.Status != "ACTIVE") return null;

    var paymentMethod = await _paymentMethodRepository.GetByIdAsync(req.PaymentMethodId, cancellationToken);

    if (paymentMethod is null) return null;

    // Begin transaction
    await using var dbTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);

    try
    {
      var referenceNo = $"TOPUP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

      var transaction = new Transaction(
        referenceNo,
        senderWalletId: null,
        receiverWalletId: wallet.Id,
        type: "TOPUP",
        req.Amount,
        status: "PENDING",
        description: $"Top up via {paymentMethod.Name}",
        completedAtUtc: null
      );

      await _db.Transactions.AddAsync(transaction, cancellationToken);
      await _db.SaveChangesAsync(cancellationToken);

      var topUpRequest = new TopUpRequest(
        transaction.Id,
        wallet.Id,
        paymentMethod.Id,
        req.Amount,
        status: "PENDING",
        providerReference: $"VA-{Guid.NewGuid().ToString("N")[..10].ToUpper()}",
        expiresAtUtc: DateTime.UtcNow.AddHours(1),
        paidAtUtc: null
      );

      await _db.TopUpRequests.AddAsync(topUpRequest, cancellationToken);
      await _db.SaveChangesAsync(cancellationToken);

      var auditLog = new AuditLog(
        userId,
        transaction.Id,
        action: "TOPUP_REQUESTED",
        entityType: "TopUpRequest",
        entityId: topUpRequest.Id.ToString(),
        outcome: "SUCCESS",
        description: $"Permintaan top up sebesar {req.Amount:C0} dibuat dengan status PENDING.",
        correlationId
      );

      await _db.AuditLogs.AddAsync(auditLog, cancellationToken);
      await _db.SaveChangesAsync(cancellationToken);

      // Commit
      await dbTransaction.CommitAsync(cancellationToken);

      return TopUpResponse.FromModel(topUpRequest, transaction, paymentMethod);
    }
    catch
    {
      // Rollback
      await dbTransaction.RollbackAsync(cancellationToken);
      _db.ChangeTracker.Clear();
      throw;
    }
  }

  public async Task<WalletResponse?> ConfirmTopUpAsync(
    long topUpRequestId,
    long actorUserId,
    string? correlationId,
    CancellationToken cancellationToken)
  {
    var actorExists = await _db.Users
    .AsNoTracking()
    .AnyAsync(u => u.Id == actorUserId, cancellationToken);

    if (!actorExists)
    {
      throw new KeyNotFoundException($"User dengan ID {actorUserId} tidak ditemukan.");
    }

    // Begin transaction
    await using var dbTransaction = await _db.Database.BeginTransactionAsync(cancellationToken);
    var isCommitted = false;

    try
    {
      var topUpRequest = await _db.TopUpRequests
        .Include(t => t.Transaction)
        .FirstOrDefaultAsync(t => t.Id == topUpRequestId, cancellationToken);

      if (topUpRequest is null) return null;

      if (topUpRequest.Status == "PAID" || topUpRequest.Transaction.Status == "SUCCESS")
      {
        throw new InvalidOperationException("Permintaan top up ini sudah lunas (PAID) dan tidak dapat diproses ulang.");
      }

      if (topUpRequest.Status != "PENDING")
      {
        throw new InvalidOperationException($"Status top up tidak valid untuk dikonfirmasi ({topUpRequest.Status}).");
      }

      if (topUpRequest.ExpiresAtUtc.HasValue && topUpRequest.ExpiresAtUtc.Value <= DateTime.UtcNow)
      {
        topUpRequest.MarkAsExpired();
        topUpRequest.Transaction.MarkAsFailed();

        var expiredLog = new AuditLog(
          actorUserId: actorUserId,
          transactionId: topUpRequest.TransactionId,
          action: "TOPUP_EXPIRED",
          entityType: "TopUpRequest",
          entityId: topUpRequest.Id.ToString(),
          outcome: "FAILED",
          description: $"Permintaan top up referensi {topUpRequest.Transaction.ReferenceNo} telah kedaluwarsa.",
          correlationId: correlationId);

        await _db.AuditLogs.AddAsync(expiredLog, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);
        isCommitted = true;

        throw new InvalidOperationException("Permintaan top up sudah kedaluwarsa (EXPIRED).");
      }

      var wallet = await _walletRepository.GetByIdAsync(topUpRequest.WalletId, cancellationToken);

      if (wallet is null || wallet.Status != "ACTIVE")
      {
        topUpRequest.MarkAsFailed();
        topUpRequest.Transaction.MarkAsFailed();

        var failedLog = new AuditLog(
          actorUserId: actorUserId,
          transactionId: topUpRequest.TransactionId,
          action: "TOPUP_FAILED",
          entityType: "TopUpRequest",
          entityId: topUpRequest.Id.ToString(),
          outcome: "FAILED",
          description: $"Konfirmasi top up referensi {topUpRequest.Transaction.ReferenceNo} gagal karena wallet tidak ACTIVE.",
          correlationId: correlationId);

        await _db.AuditLogs.AddAsync(failedLog, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);
        isCommitted = true;

        throw new InvalidOperationException("Wallet tidak ditemukan atau tidak berstatus ACTIVE.");
      }

      var balanceBefore = wallet.Balance;
      wallet.IncreaseBalance(topUpRequest.Amount);
      var balanceAfter = wallet.Balance;

      topUpRequest.MarkAsPaid();
      topUpRequest.Transaction.MarkAsSuccess();

      var mutation = new WalletMutation(
        transactionId: topUpRequest.TransactionId,
        walletId: wallet.Id,
        direction: "IN",
        amount: topUpRequest.Amount,
        balanceBefore: balanceBefore,
        balanceAfter: balanceAfter);

      await _db.WalletMutations.AddAsync(mutation, cancellationToken);

      var auditLog = new AuditLog(
        actorUserId: actorUserId,
        transactionId: topUpRequest.TransactionId,
        action: "TOPUP_PAID",
        entityType: "Wallet",
        entityId: wallet.Id.ToString(),
        outcome: "SUCCESS",
        description: $"Top up referensi {topUpRequest.Transaction.ReferenceNo} berhasil dikonfirmasi.",
        correlationId: correlationId);

      await _db.AuditLogs.AddAsync(auditLog, cancellationToken);

      await _db.SaveChangesAsync(cancellationToken);

      // Commit
      await dbTransaction.CommitAsync(cancellationToken);
      isCommitted = true;

      return WalletResponse.FromModel(wallet);
    }
    catch
    {
      if (!isCommitted)
      {
        await dbTransaction.RollbackAsync(cancellationToken);
        _db.ChangeTracker.Clear();
      }

      throw;
    }
  }
}