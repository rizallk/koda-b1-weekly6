using Weekly6.DTOs;
using Weekly6.Models;

namespace Weekly6.Repositories;

public interface IUserRepository
{
  Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken);
  Task<UserWallet?> GetByIdAsync(long id, CancellationToken cancellationToken);
  Task<bool> TryAddAsync(User user, CancellationToken cancellationToken);
  Task<bool> TryUpdateAsync(long id, User user, CancellationToken cancellationToken);
}