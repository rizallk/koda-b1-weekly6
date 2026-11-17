using Weekly6.DTOs;
using Weekly6.Models;

namespace Weekly6.Repositories;

public interface IPaymentMethodRepository
{
  Task<IReadOnlyList<PaymentMethod>> GetAllAsync(PaymentMethodFilterRequest filter, CancellationToken cancellationToken);
  Task<PaymentMethod?> GetByIdAsync(long id, CancellationToken cancellationToken);
}
