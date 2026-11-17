using Weekly6.DTOs;

namespace Weekly6.Services;

public interface IPaymentMethodService
{
  Task<IReadOnlyList<PaymentMethodResponse>> GetAllAsync(PaymentMethodFilterRequest filter, CancellationToken cancellationToken);
}