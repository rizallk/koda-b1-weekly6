using Weekly6.DTOs;
using Weekly6.Models;
using Weekly6.Repositories;

namespace Weekly6.Services;

public sealed class UserService(IUserRepository userRepository, IWalletRepository walletRepository, ITransactionRepository transactionRepository, IWalletMutationRepository walletMutationRepository) : IUserService
{
  private readonly IUserRepository _userRepository = userRepository;
  private readonly IWalletRepository _walletRepository = walletRepository;
  private readonly ITransactionRepository _transactionRepository = transactionRepository;
  private readonly IWalletMutationRepository _walletMutationRepository = walletMutationRepository;

  public async Task<UserResponse?> CreateAsync(UserRequest req, CancellationToken cancellationToken)
  {
    string generatedWalletNumber = "W001";
    string? lastWalletNumber = await _walletRepository.GetLastWalletNumberAsync(cancellationToken);

    if (!string.IsNullOrWhiteSpace(lastWalletNumber) && lastWalletNumber.StartsWith('W'))
    {
      string numberPart = lastWalletNumber.Substring(1);

      if (int.TryParse(numberPart, out int lastNumber))
      {
        int newNumber = lastNumber + 1;
        generatedWalletNumber = $"W{newNumber:D3}";
      }
    }

    var user = new User(req.Username, req.FullName, req.Email, req.Phone, null, "ACTIVE", null);

    user.AssignWallet(generatedWalletNumber);

    var newUser = await _userRepository.TryAddAsync(user, cancellationToken);

    return newUser ? UserResponse.FromModel(user) : null;
  }

  public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken)
  {
    var users = await _userRepository.GetAllAsync(cancellationToken);

    return users.Select(UserResponse.FromModel).ToArray();
  }

  public async Task<UserWallet?> GetByIdAsync(long id, CancellationToken cancellationToken)
  {
    if (id <= 0)
    {
      return null;
    }
    var user = await _userRepository.GetByIdAsync(id, cancellationToken);

    return user is null ? null : user;
  }

  public async Task<UserResponse?> UpdateAsync(long id, UserUpdateRequest req, CancellationToken cancellationToken)
  {
    var userExists = await _userRepository.GetByIdAsync(id, cancellationToken);

    if (userExists is null) return null;

    User user;
    string? newPasswordHash = null;

    if (!string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword is not null)
    {
      newPasswordHash = req.NewPassword; // hashing password

      user = new User(userExists.UserId, req.Username, req.FullName, req.Email, req.Phone, newPasswordHash, userExists.Status, DateTime.UtcNow);
    }
    else
    {
      user = new User(userExists.UserId, req.Username, req.FullName, req.Email, req.Phone, newPasswordHash, userExists.Status, DateTime.UtcNow);
    }

    var updated = await _userRepository.TryUpdateAsync(id, user, cancellationToken);

    return updated ? UserResponse.FromModel(user) : null;
  }

  public async Task<PagedResponse<TransactionResponse>?> GetUserTransactionsAsync(
    long userId,
    TransactionFilterRequest filter,
    CancellationToken cancellationToken)
  {
    if (userId <= 0)
    {
      return null;
    }

    var wallet = await _walletRepository.GetReadOnlyByUserIdAsync(userId, cancellationToken);
    if (wallet is null)
    {
      return null;
    }

    var (items, totalCount) = await _transactionRepository.GetTransactionHistoryAsync(
      wallet.Id,
      filter,
      cancellationToken);

    var page = filter.Page < 1 ? 1 : filter.Page;
    var pageSize = filter.PageSize is < 1 or > 100 ? 10 : filter.PageSize;

    return new PagedResponse<TransactionResponse>(items, page, pageSize, totalCount);
  }

  public async Task<PagedResponse<WalletMutationResponse>?> GetUserMutationsAsync(
    long userId,
    WalletMutationFilterRequest filter,
    CancellationToken cancellationToken)
  {
    if (userId <= 0)
    {
      return null;
    }

    var wallet = await _walletRepository.GetReadOnlyByUserIdAsync(userId, cancellationToken);
    if (wallet is null)
    {
      return null;
    }

    var (items, totalCount) = await _walletMutationRepository.GetWalletMutationsAsync(
      wallet.Id,
      filter,
      cancellationToken);

    var page = filter.Page < 1 ? 1 : filter.Page;
    var pageSize = filter.PageSize is < 1 or > 100 ? 10 : filter.PageSize;

    return new PagedResponse<WalletMutationResponse>(items, page, pageSize, totalCount);
  }
}