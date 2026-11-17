using Weekly6.DTOs;
using Weekly6.Models;

namespace Weekly6.Repositories;

public interface IWalletMutationRepository
{
  Task<(IReadOnlyList<WalletMutationResponse> Items, int TotalCount)> GetWalletMutationsAsync(
    long walletId,
    WalletMutationFilterRequest filter,
    CancellationToken cancellationToken);
}