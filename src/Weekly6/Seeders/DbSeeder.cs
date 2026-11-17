using Weekly6.Data;

namespace Weekly6.Seeders;

public static class DbSeeder
{
    public static void Seed(AppDbContext context)
    {
        using var dbTransaction = context.Database.BeginTransaction();

        try
        {
            var paymentMethods = PaymentMethodSeeder.Seed(context);
            var users = UserWalletSeeder.Seed(context);

            TopUpSeeder.Seed(context, users, paymentMethods);
            TransferSeeder.Seed(context, users);

            dbTransaction.Commit();
        }
        catch
        {
            dbTransaction.Rollback();
            throw;
        }
    }

    public static async Task SeedAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        await using var dbTransaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var paymentMethods = await PaymentMethodSeeder.SeedAsync(context, cancellationToken);
            var users = await UserWalletSeeder.SeedAsync(context, cancellationToken);

            await TopUpSeeder.SeedAsync(context, users, paymentMethods, cancellationToken);
            await TransferSeeder.SeedAsync(context, users, cancellationToken);

            await dbTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}