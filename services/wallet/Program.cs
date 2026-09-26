using Microsoft.EntityFrameworkCore;
using wallet.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MassTransit;
using Contracts.Grpc;
using wallet.Consumers;
using wallet.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddGrpcClient<AccountService.AccountServiceClient>(o =>
    o.Address = new Uri(builder.Configuration["Services:Account:GrpcUrl"]!));

builder.Services.AddScoped<IAccountNumberGenerator, AccountNumberGenerator>();

builder.Services.AddControllers();
// builder.Services.AddGrpc();

var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMq:Username"] ?? "admin";
var rabbitPass = builder.Configuration["RabbitMq:Password"] ?? "password123";

builder.Services.AddMassTransit(x =>
{
    // Without a per-service prefix both services name their queue "UserRegistered"
    // and compete for the same messages instead of each getting a copy.
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("wallet", false));

    x.AddEntityFrameworkOutbox<ApplicationDbContext>(o =>
    {
        o.QueryDelay = TimeSpan.FromSeconds(10);
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddConsumer<UserRegisteredConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
