using Weekly6.DTOs;
using Weekly6.Models;

namespace Weekly6.Services;

public interface IWalletService
{
  Task<WalletResponse?> GetByUserIdAsync(long userId, CancellationToken cancellationToken);
  Task<Wallet?> GetByIdAsync(long id, CancellationToken cancellationToken);
  Task<Wallet?> GetByWalletNumberAsync(string walletNumber, CancellationToken cancellationToken);
  Task<TopUpResponse?> RequestTopUpAsync(long userId, TopUpRequestDto request, string? correlationId, CancellationToken cancellationToken);
  public Task<WalletResponse?> ConfirmTopUpAsync(
   long topUpRequestId,
   long actorUserId,
   string? correlationId,
   CancellationToken cancellationToken);
}