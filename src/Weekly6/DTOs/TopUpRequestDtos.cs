using Weekly6.Models;

namespace Weekly6.DTOs;

public sealed record TopUpRequestDto(
  long PaymentMethodId,
  decimal Amount
);

public sealed record TopUpResponse(
  long walletId,
  long TopUpRequestId,
  long TransactionId,
  string ReferenceNo,
  string PaymentMethodCode,
  string PaymentMethodName,
  string? ProviderReference,
  decimal Amount,
  string Status,
  DateTime CreatedAtUtc,
  DateTime? ExpiresAtUtc
)
{
  public static TopUpResponse FromModel(
    TopUpRequest topUpRequest,
    Transaction transaction,
    PaymentMethod paymentMethod)
  {
    return new TopUpResponse(
      walletId: topUpRequest.WalletId,
      TopUpRequestId: topUpRequest.Id,
      TransactionId: transaction.Id,
      ReferenceNo: transaction.ReferenceNo,
      PaymentMethodCode: paymentMethod.Code,
      PaymentMethodName: paymentMethod.Name,
      ProviderReference: topUpRequest.ProviderReference,
      Amount: topUpRequest.Amount,
      Status: topUpRequest.Status,
      CreatedAtUtc: topUpRequest.CreatedAtUtc,
      ExpiresAtUtc: topUpRequest.ExpiresAtUtc
    );
  }
}