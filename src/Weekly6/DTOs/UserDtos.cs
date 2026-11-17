
using System.ComponentModel.DataAnnotations;
using Weekly6.Models;

namespace Weekly6.DTOs;

public sealed record UserRequest(
  [Required, StringLength(50)]
  string Username,

  [Required, StringLength(100)]
  string FullName,

  [Required, StringLength(150), EmailAddress]
  string Email,

  [Required, StringLength(20), Phone]
  string Phone
);

public sealed record UserResponse(
  long Id,
  string Username,
  string FullName,
  string Email,
  string Phone,
  string Status,
  DateTime CreatedAtUtc,
  DateTime? UpdatedAtUtc)
{
  public static UserResponse FromModel(User user) => new(user.Id, user.Username, user.FullName, user.Email, user.Phone, user.Status, user.CreatedAtUtc, user.UpdatedAtUtc);
}

public record UserTransaction(
  long TransactionId,
  DateTime CreatedAt,
  string Type,
  decimal Amount,
  string? Description
);

public sealed record UserUpdateRequest(
  [Required, StringLength(50)]
  string Username,

  [Required, StringLength(100)]
  string FullName,

  [Required, StringLength(150), EmailAddress]
  string Email,

  [Required, StringLength(20), Phone]
  string Phone,

  [StringLength(500)]
  string? NewPassword
);

public record UserWallet(
  long UserId,
  string Username,
  string FullName,
  string Email,
  string Phone,
  string Status,
  long WalletId,
  string WalletNumber,
  decimal Balance
);