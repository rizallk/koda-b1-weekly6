using Weekly6.DTOs;

namespace Weekly6.Services;

public interface IUserService
{
  Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken);
  Task<UserWallet?> GetByIdAsync(long id, CancellationToken cancellationToken);
  Task<UserResponse?> CreateAsync(UserRequest request, CancellationToken cancellationToken);
  Task<UserResponse?> UpdateAsync(long id, UserUpdateRequest request, CancellationToken cancellationToken);
  Task<PagedResponse<TransactionResponse>?> GetUserTransactionsAsync(long userId, TransactionFilterRequest filter, CancellationToken cancellationToken);
  Task<PagedResponse<WalletMutationResponse>?> GetUserMutationsAsync(
    long userId,
    WalletMutationFilterRequest filter,
    CancellationToken cancellationToken);
}