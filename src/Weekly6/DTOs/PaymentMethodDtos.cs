using Weekly6.Models;

namespace Weekly6.DTOs;

public sealed record PaymentMethodResponse(
  long Id,
  string Code,
  string Name,
  string? ProviderName,
  bool IsActive,
  DateTime CreatedAtUtc
)
{
  public static PaymentMethodResponse FromModel(PaymentMethod pm) => new(pm.Id, pm.Code, pm.Name, pm.ProviderName, pm.IsActive, pm.CreatedAtUtc);
};

public sealed record PaymentMethodFilterRequest(
  bool? IsActive = null
);

