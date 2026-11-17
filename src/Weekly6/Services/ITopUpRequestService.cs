using Weekly6.DTOs;

namespace Weekly6.Services;

public interface ITopUpRequestService
{
  Task<IReadOnlyList<TopUpResponse>> GetAllAsync(CancellationToken cancellationToken);
}