using Weekly6.DTOs;
using Weekly6.Repositories;

namespace Weekly6.Services;

public sealed class PaymentMethodService(IPaymentMethodRepository repository) : IPaymentMethodService
{
  private readonly IPaymentMethodRepository _repository = repository;

  public async Task<IReadOnlyList<PaymentMethodResponse>> GetAllAsync(PaymentMethodFilterRequest filter, CancellationToken cancellationToken)
  {
    var paymentMethods = await _repository.GetAllAsync(filter, cancellationToken);

    return paymentMethods.Select(PaymentMethodResponse.FromModel).ToArray();
  }
}