using Microsoft.EntityFrameworkCore;
using Weekly6.Data;
using Weekly6.Models;

namespace Weekly6.Repositories;

public sealed class TopUpRequestRepository(AppDbContext db) : ITopUpRequestRepository
{
  private readonly AppDbContext _db = db;

  public async Task<IReadOnlyList<TopUpRequest>> GetAllAsync(CancellationToken cancellationToken)
  {
    return await _db.TopUpRequests
     .AsNoTracking()
     .Include(p => p.Transaction)
     .Include(p => p.PaymentMethod)
     .OrderByDescending(p => p.CreatedAtUtc)
     .ToListAsync(cancellationToken);
  }
}