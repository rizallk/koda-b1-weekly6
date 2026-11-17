using Microsoft.EntityFrameworkCore;
using Weekly6.Data;
using Weekly6.Models;

namespace Weekly6.Seeders;

public static class UserWalletSeeder
{
  public static List<User> Seed(AppDbContext context)
  {
    if (context.Users.Any())
    {
      return context.Users
          .Include(u => u.Wallet)
          .OrderBy(u => u.Id)
          .ToList();
    }

    var users = BuildInitialUsers();
    context.Users.AddRange(users);
    context.SaveChanges();

    return users;
  }

  public static async Task<List<User>> SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
  {
    if (await context.Users.AnyAsync(cancellationToken))
    {
      return await context.Users
          .Include(u => u.Wallet)
          .OrderBy(u => u.Id)
          .ToListAsync(cancellationToken);
    }

    var users = BuildInitialUsers();
    await context.Users.AddRangeAsync(users, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);

    return users;
  }

  private static List<User> BuildInitialUsers()
  {
    var usersData = new List<(string Username, string FullName, string Email, string Phone, string Status)>
        {
            ("rizal.kadamong", "Mohalim Rizal Kadamong", "rizal@example.com", "081234567801", "ACTIVE"),
            ("budi.santoso", "Budi Santoso", "budi@example.com", "081234567802", "ACTIVE"),
            ("siti.aminah", "Siti Aminah", "siti@example.com", "081234567803", "ACTIVE"),
            ("andi.pratama", "Andi Pratama", "andi@example.com", "081234567804", "ACTIVE"),
            ("dewi.lestari", "Dewi Lestari", "dewi@example.com", "081234567805", "ACTIVE"),
            ("eko.saputra", "Eko Saputra", "eko@example.com", "081234567806", "ACTIVE"),
            ("fitri.handayani", "Fitri Handayani", "fitri@example.com", "081234567807", "ACTIVE"),
            ("gilang.ramadhan", "Gilang Ramadhan", "gilang@example.com", "081234567808", "ACTIVE"),
            ("hani.puspita", "Hani Puspita", "hani@example.com", "081234567809", "ACTIVE"),
            ("irfan.hakim", "Irfan Hakim", "irfan@example.com", "081234567810", "SUSPENDED")
        };

    var users = new List<User>();
    for (int i = 0; i < usersData.Count; i++)
    {
      var data = usersData[i];
      var user = new User(
          username: data.Username,
          fullName: data.FullName,
          email: data.Email,
          phone: data.Phone,
          passwordHash: null,
          status: data.Status,
          updatedAtUtc: null
      );

      user.AssignWallet($"W{i + 1:D3}");
      users.Add(user);
    }

    return users;
  }
}