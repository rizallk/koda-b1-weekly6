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
     .Include(t => t.Transaction)
     .Include(t => t.PaymentMethod)
     .OrderByDescending(t => t.CreatedAtUtc)
     .ToListAsync(cancellationToken);
  }
}