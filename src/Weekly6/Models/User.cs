namespace Weekly6.Models;

public sealed class User
{
  public long Id { get; private set; }
  public string Username { get; private set; } = null!;
  public string FullName { get; private set; } = null!;
  public string Email { get; private set; } = null!;
  public string Phone { get; private set; } = null!;
  public string? PasswordHash { get; private set; }
  public string Status { get; private set; } = null!;
  public DateTime CreatedAtUtc { get; private set; }
  public DateTime? UpdatedAtUtc { get; private set; }

  public ICollection<AuditLog> AuditLogs { get; private set; } = [];
  public Wallet Wallet { get; private set; } = null!;

  private User() { }

  public User(string username, string fullName, string email, string phone, string? passwordHash, string status, DateTime? updatedAtUtc)
  {
    if (string.IsNullOrWhiteSpace(username))
    {
      throw new ArgumentException("Username wajib diisi", nameof(username));
    }
    if (string.IsNullOrWhiteSpace(fullName))
    {
      throw new ArgumentException("Nama lengkap wajib diisi", nameof(fullName));
    }
    if (string.IsNullOrWhiteSpace(email))
    {
      throw new ArgumentException("Email wajib diisi", nameof(email));
    }
    if (string.IsNullOrWhiteSpace(phone))
    {
      throw new ArgumentException("Nomor HP wajib diisi", nameof(phone));
    }
    if (string.IsNullOrWhiteSpace(status))
    {
      throw new ArgumentException("Status wajib diisi", nameof(status));
    }

    Username = username;
    FullName = fullName;
    Email = email;
    Phone = phone;
    if (!string.IsNullOrWhiteSpace(passwordHash) || passwordHash is not null) PasswordHash = passwordHash;
    Status = status;
    CreatedAtUtc = DateTime.UtcNow;
    UpdatedAtUtc = updatedAtUtc;
  }

  public User(long id, string username, string fullName, string email, string phone, string? passwordHash, string status, DateTime? updatedAtUtc)
  {
    if (id <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(id), "Id tidak boleh 0 atau negatif");
    }
    if (string.IsNullOrWhiteSpace(username))
    {
      throw new ArgumentException("Username wajib diisi", nameof(username));
    }
    if (string.IsNullOrWhiteSpace(fullName))
    {
      throw new ArgumentException("Nama lengkap wajib diisi", nameof(fullName));
    }
    if (string.IsNullOrWhiteSpace(email))
    {
      throw new ArgumentException("Email wajib diisi", nameof(email));
    }
    if (string.IsNullOrWhiteSpace(phone))
    {
      throw new ArgumentException("Nomor HP wajib diisi", nameof(phone));
    }
    if (string.IsNullOrWhiteSpace(status))
    {
      throw new ArgumentException("Status wajib diisi", nameof(status));
    }

    Id = id;
    Username = username;
    FullName = fullName;
    Email = email;
    Phone = phone;
    if (!string.IsNullOrWhiteSpace(passwordHash) || passwordHash is not null) PasswordHash = passwordHash;
    Status = status;
    UpdatedAtUtc = updatedAtUtc;
  }

  public void AssignWallet(string walletNumber)
  {
    if (Wallet != null)
      throw new InvalidOperationException("User sudah memiliki wallet.");

    Wallet = new Wallet(walletNumber, 0, "ACTIVE", null);
  }

  public void UpdateDetail(string username, string fullName, string email, string phone, string? passwordHash, string status)
  {
    var validated = new User(username, fullName, email, phone, passwordHash, status, null);

    Username = validated.Username;
    FullName = validated.FullName;
    Email = validated.Email;
    Phone = validated.Phone;
    PasswordHash = validated.PasswordHash;
    Status = validated.Status;
    UpdatedAtUtc = DateTime.UtcNow;
  }
}