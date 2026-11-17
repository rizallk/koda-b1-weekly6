using Weekly6.Models;

namespace Weekly6.Repositories;

public interface IWalletRepository
{
  Task<Wallet?> GetByUserIdAsync(long userId, CancellationToken cancellationToken);
  Task<Wallet?> GetReadOnlyByUserIdAsync(long id, CancellationToken cancellationToken);
  Task<bool> TryAddAsync(Wallet wallet, CancellationToken cancellationToken);
  Task<bool> TryUpdateBalanceAsync(Wallet wallet, CancellationToken cancellationToken);
  Task<string?> GetLastWalletNumberAsync(CancellationToken cancellationToken);
  Task<Wallet?> GetByIdAsync(long id, CancellationToken cancellationToken);
  Task<Wallet?> GetReadOnlyByIdAsync(long id, CancellationToken cancellationToken);
  Task<Wallet?> GetByWalletNumberAsync(string walletNumber, CancellationToken cancellationToken);
}