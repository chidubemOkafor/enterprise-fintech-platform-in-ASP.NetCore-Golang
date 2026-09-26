using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MassTransit;
using account.Data;
using account.Consumers;
using account.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICES ====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddGrpc();
builder.Services.AddHealthChecks();

var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMq:Username"] ?? "admin";
var rabbitPass = builder.Configuration["RabbitMq:Password"] ?? "password123";

builder.Services.AddMassTransit(x =>
{
    // Without a per-service prefix both services name their queue "UserRegistered"
    // and compete for the same messages instead of each getting a copy.
    x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("account", false));

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

// ===== BUILD (the divider) =====
var app = builder.Build();

// ===== MIDDLEWARE =====

app.MapGrpcService<AccountGrpcService>();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();