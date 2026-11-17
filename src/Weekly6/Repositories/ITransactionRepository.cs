using Weekly6.DTOs;
using Weekly6.Models;

namespace Weekly6.Repositories;

public interface ITransactionRepository
{
  Task<bool> TryAddAsync(Transaction trx, CancellationToken cancellationToken);
  Task<Transaction?> GetByIdAsync(long id, CancellationToken cancellationToken);
  Task<Transaction?> GetReadOnlyByReferenceNoAsync(string referenceNo, CancellationToken cancellationToken);
  Task<IReadOnlyList<TransactionSummary>> GetTransactionSummaryByWalletIdAsync(long walletId, CancellationToken cancellationToken);
  Task<(IReadOnlyList<TransactionResponse> Items, int TotalCount)> GetTransactionHistoryAsync(
    long walletId,
    TransactionFilterRequest filter,
    CancellationToken cancellationToken);
}