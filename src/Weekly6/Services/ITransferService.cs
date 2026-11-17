using Weekly6.DTOs;

namespace Weekly6.Services;

public interface ITransferService
{
  Task<TransactionResponse?> TransferAsync(TransferRequest transfer, string? correlationId, CancellationToken cancellationToken);
}