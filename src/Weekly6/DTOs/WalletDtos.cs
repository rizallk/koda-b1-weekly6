using System.ComponentModel.DataAnnotations;
using Weekly6.Models;

namespace Weekly6.DTOs;

public record WalletRequest(
  [Required]
  int UserId,

  [Required]
  [StringLength(20)]
  string WalletNumber,

  [Required]
  [Range(typeof(decimal), "0", "99999999999.99", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true)]
  decimal Balance
);

public sealed record WalletResponse(
  long Id,
  long UserId,
  string WalletNumber,
  decimal Balance,
  string Status,
  DateTime CreatedAtUtc,
  DateTime? UpdatedAtUtc
)
{
  public static WalletResponse FromModel(Wallet wallet) => new(wallet.Id, wallet.UserId, wallet.WalletNumber, wallet.Balance, wallet.Status, wallet.CreatedAtUtc, wallet.UpdatedAtUtc);
}
