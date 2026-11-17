using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Weekly6.Data;
using Weekly6.DTOs;
using Weekly6.Models;

namespace Weekly6.Repositories;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
  private readonly AppDbContext _db = db;

  public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken)
  {
    return await _db.Users.AsNoTracking().OrderBy(u => u.CreatedAtUtc).ToListAsync(cancellationToken);
  }

  public async Task<UserWallet?> GetByIdAsync(long id, CancellationToken cancellationToken)
  {
    return await _db.Users
      .AsNoTracking()
      .Where(u => u.Id == id)
      .Select(u => new UserWallet(
        u.Id,
        u.Username,
        u.FullName,
        u.Email,
        u.Phone,
        u.Status,
        u.Wallet.Id,
        u.Wallet.WalletNumber,
        u.Wallet.Balance
      ))
      .FirstOrDefaultAsync(cancellationToken);
  }

  public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken)
  {
    _db.Users.Add(user);
    await _db.SaveChangesAsync(cancellationToken);

    if (user is null) return false;

    return true;
  }

  public async Task<bool> TryDeleteAsync(long id, CancellationToken cancellationToken)
  {
    var existing = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    if (existing is null) return false;

    _db.Users.Remove(existing);

    await _db.SaveChangesAsync(cancellationToken);

    return true;
  }

  public async Task<bool> TryUpdateAsync(long id, User user, CancellationToken cancellationToken)
  {
    var existing = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    if (existing is null) return false;

    existing.UpdateDetail(
      user.Username,
      user.FullName,
      user.Email,
      user.Phone,
      user.PasswordHash,
      user.Status
    );

    await _db.SaveChangesAsync(cancellationToken);
    return true;
  }
}