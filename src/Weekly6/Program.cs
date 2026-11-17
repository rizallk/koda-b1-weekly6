using ErrorHandling.Middlewares;
using Microsoft.EntityFrameworkCore;
using Weekly6.Data;
using Weekly6.Middlewares;
using Weekly6.Repositories;
using Weekly6.Seeders;
using Weekly6.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(
    option =>
    {
      option.CustomizeProblemDetails = context =>
      {
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path.Value;

        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
      };
    }
);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Repository
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IWalletMutationRepository, WalletMutationRepository>();
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<ITopUpRequestRepository, TopUpRequestRepository>();

// Service
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<ITransferService, TransferService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IPaymentMethodService, PaymentMethodService>();
builder.Services.AddScoped<ITopUpRequestService, TopUpRequestService>();

// Db Context
var connectionString = builder.Configuration.GetConnectionString("ewallet") ?? throw new InvalidOperationException("ConnectionString : Belum dikonfigurasi");

builder.Services.AddDbContext<AppDbContext>(options =>
{
  options.UseSqlServer(connectionString)
    .UseSeeding((context, _) =>
    {
      DbSeeder.Seed((AppDbContext)context);
    })
    .UseAsyncSeeding(async (context, _, cancellationToken) =>
    {
      await DbSeeder.SeedAsync((AppDbContext)context, cancellationToken);
    });
});

var app = builder.Build();

app.UseExceptionHandler();

app.UseCorrelationId();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
  app.MapOpenApi();
  app.UseSwaggerUI(options =>
   {
     options.SwaggerEndpoint("/openapi/v1.json", "E-Wallet API v1");
     options.RoutePrefix = "swagger";

     options.DocumentTitle = "E-Wallet | API Documentation";

     options.EnableFilter();
   });
}

app.UseHttpsRedirection();

app.UseRouting();

app.MapControllers();

app.Run();
