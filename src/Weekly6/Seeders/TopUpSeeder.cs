using Microsoft.EntityFrameworkCore;
using Weekly6.Data;
using Weekly6.Models;

namespace Weekly6.Seeders;

public static class TopUpSeeder
{
  public static void Seed(
      AppDbContext context,
      List<User> users,
      Dictionary<string, PaymentMethod> paymentMethods)
  {
    if (context.TopUpRequests.Any())
    {
      return;
    }

    var bankTransferId = paymentMethods["BANK_TRANSFER"].Id;
    var qrisId = paymentMethods["QRIS"].Id;

    CreateSuccessTopUp(context, users[0], bankTransferId, 1_000_000m, "TOPUP-20261115-0001", "VA-BCA-88001122", "SEED-CORR-001");
    CreateSuccessTopUp(context, users[1], qrisId, 500_000m, "TOPUP-20261115-0002", "QRIS-GPN-99001122", "SEED-CORR-002");
    CreateSuccessTopUp(context, users[2], bankTransferId, 750_000m, "TOPUP-20261115-0003", "VA-MDR-77001122", "SEED-CORR-003");
    CreateSuccessTopUp(context, users[3], qrisId, 300_000m, "TOPUP-20261115-0004", "QRIS-GPN-99003344", "SEED-CORR-004");
    CreatePendingTopUp(context, users[4], bankTransferId, 250_000m, "TOPUP-20261115-0005", "VA-BCA-88005566", "SEED-CORR-005");
  }

  public static async Task SeedAsync(
      AppDbContext context,
      List<User> users,
      Dictionary<string, PaymentMethod> paymentMethods,
      CancellationToken cancellationToken = default)
  {
    if (await context.TopUpRequests.AnyAsync(cancellationToken))
    {
      return;
    }

    var bankTransferId = paymentMethods["BANK_TRANSFER"].Id;
    var qrisId = paymentMethods["QRIS"].Id;

    await CreateSuccessTopUpAsync(context, users[0], bankTransferId, 1_000_000m, "TOPUP-20261115-0001", "VA-BCA-88001122", "SEED-CORR-001", cancellationToken);
    await CreateSuccessTopUpAsync(context, users[1], qrisId, 500_000m, "TOPUP-20261115-0002", "QRIS-GPN-99001122", "SEED-CORR-002", cancellationToken);
    await CreateSuccessTopUpAsync(context, users[2], bankTransferId, 750_000m, "TOPUP-20261115-0003", "VA-MDR-77001122", "SEED-CORR-003", cancellationToken);
    await CreateSuccessTopUpAsync(context, users[3], qrisId, 300_000m, "TOPUP-20261115-0004", "QRIS-GPN-99003344", "SEED-CORR-004", cancellationToken);
    await CreatePendingTopUpAsync(context, users[4], bankTransferId, 250_000m, "TOPUP-20261115-0005", "VA-BCA-88005566", "SEED-CORR-005", cancellationToken);
  }

  // --- SYNC HELPERS ---
  private static void CreateSuccessTopUp(
      AppDbContext context, User user, int paymentMethodId, decimal amount,
      string referenceNo, string providerRef, string correlationId)
  {
    var wallet = user.Wallet;
    var now = DateTime.UtcNow;

    var trx = new Transaction(referenceNo, null, wallet.Id, "TOPUP", amount, "SUCCESS", $"Top up saldo sebesar Rp {amount:N0}", now);
    context.Transactions.Add(trx);
    context.SaveChanges();

    var topUpReq = new TopUpRequest(trx.Id, wallet.Id, paymentMethodId, amount, "PAID", providerRef, now.AddHours(24), now);
    context.TopUpRequests.Add(topUpReq);

    decimal balanceBefore = wallet.Balance;
    wallet.IncreaseBalance(amount);
    decimal balanceAfter = wallet.Balance;

    var mutation = new WalletMutation(trx.Id, wallet.Id, "IN", amount, balanceBefore, balanceAfter);
    context.WalletMutations.Add(mutation);

    context.AuditLogs.AddRange(
        new AuditLog(user.Id, trx.Id, "TOPUP_REQUESTED", "TopUpRequest", trx.Id.ToString(), "SUCCESS", $"Permintaan top up {referenceNo} dibuat.", correlationId),
        new AuditLog(user.Id, trx.Id, "TOPUP_PAID", "Transaction", trx.Id.ToString(), "SUCCESS", $"Top up {referenceNo} berhasil dibayar. Saldo bertambah Rp {amount:N0}.", correlationId)
    );

    context.SaveChanges();
  }

  private static void CreatePendingTopUp(
      AppDbContext context, User user, int paymentMethodId, decimal amount,
      string referenceNo, string providerRef, string correlationId)
  {
    var wallet = user.Wallet;
    var now = DateTime.UtcNow;

    var trx = new Transaction(referenceNo, null, wallet.Id, "TOPUP", amount, "PENDING", $"Menunggu pembayaran top up Rp {amount:N0}", null);
    context.Transactions.Add(trx);
    context.SaveChanges();

    var topUpReq = new TopUpRequest(trx.Id, wallet.Id, paymentMethodId, amount, "PENDING", providerRef, now.AddHours(24), null);
    context.TopUpRequests.Add(topUpReq);

    var auditRequested = new AuditLog(user.Id, trx.Id, "TOPUP_REQUESTED", "TopUpRequest", trx.Id.ToString(), "SUCCESS", $"Permintaan top up {referenceNo} menunggu pembayaran.", correlationId);
    context.AuditLogs.Add(auditRequested);

    context.SaveChanges();
  }

  // --- ASYNC HELPERS ---
  private static async Task CreateSuccessTopUpAsync(
      AppDbContext context, User user, int paymentMethodId, decimal amount,
      string referenceNo, string providerRef, string correlationId, CancellationToken cancellationToken)
  {
    var wallet = user.Wallet;
    var now = DateTime.UtcNow;

    var trx = new Transaction(referenceNo, null, wallet.Id, "TOPUP", amount, "SUCCESS", $"Top up saldo sebesar Rp {amount:N0}", now);
    await context.Transactions.AddAsync(trx, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);

    var topUpReq = new TopUpRequest(trx.Id, wallet.Id, paymentMethodId, amount, "PAID", providerRef, now.AddHours(24), now);
    await context.TopUpRequests.AddAsync(topUpReq, cancellationToken);

    decimal balanceBefore = wallet.Balance;
    wallet.IncreaseBalance(amount);
    decimal balanceAfter = wallet.Balance;

    var mutation = new WalletMutation(trx.Id, wallet.Id, "IN", amount, balanceBefore, balanceAfter);
    await context.WalletMutations.AddAsync(mutation, cancellationToken);

    await context.AuditLogs.AddRangeAsync(
        [
            new AuditLog(user.Id, trx.Id, "TOPUP_REQUESTED", "TopUpRequest", trx.Id.ToString(), "SUCCESS", $"Permintaan top up {referenceNo} dibuat.", correlationId),
                new AuditLog(user.Id, trx.Id, "TOPUP_PAID", "Transaction", trx.Id.ToString(), "SUCCESS", $"Top up {referenceNo} berhasil dibayar. Saldo bertambah Rp {amount:N0}.", correlationId)
        ],
        cancellationToken
    );

    await context.SaveChangesAsync(cancellationToken);
  }

  private static async Task CreatePendingTopUpAsync(
      AppDbContext context, User user, int paymentMethodId, decimal amount,
      string referenceNo, string providerRef, string correlationId, CancellationToken cancellationToken)
  {
    var wallet = user.Wallet;
    var now = DateTime.UtcNow;

    var trx = new Transaction(referenceNo, null, wallet.Id, "TOPUP", amount, "PENDING", $"Menunggu pembayaran top up Rp {amount:N0}", null);
    await context.Transactions.AddAsync(trx, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);

    var topUpReq = new TopUpRequest(trx.Id, wallet.Id, paymentMethodId, amount, "PENDING", providerRef, now.AddHours(24), null);
    await context.TopUpRequests.AddAsync(topUpReq, cancellationToken);

    var auditRequested = new AuditLog(user.Id, trx.Id, "TOPUP_REQUESTED", "TopUpRequest", trx.Id.ToString(), "SUCCESS", $"Permintaan top up {referenceNo} menunggu pembayaran.", correlationId);
    await context.AuditLogs.AddAsync(auditRequested, cancellationToken);

    await context.SaveChangesAsync(cancellationToken);
  }
}