using Weekly6.DTOs;

namespace Weekly6.Services;

public interface ITransactionService
{
  Task<TotalTransaction?> GetTransactionSummaryByWalletIdAsync(long walletId, CancellationToken cancellationToken);
}