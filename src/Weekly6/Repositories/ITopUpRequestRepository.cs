using Weekly6.DTOs;
using Weekly6.Models;

namespace Weekly6.Repositories;

public interface ITopUpRequestRepository
{
  Task<IReadOnlyList<TopUpRequest>> GetAllAsync(CancellationToken cancellationToken);
}
