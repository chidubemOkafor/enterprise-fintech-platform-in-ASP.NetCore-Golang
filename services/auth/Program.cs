using Microsoft.EntityFrameworkCore;
using auth.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICES =====
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddHealthChecks();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = false
        };
    });

builder.Services.AddAuthorization();

var rabbitHost = builder.Configuration["RabbitMq:Host"] ?? "localhost";
var rabbitUser = builder.Configuration["RabbitMq:Username"] ?? "admin";
var rabbitPass = builder.Configuration["RabbitMq:Password"] ?? "password123";

builder.Services.AddMassTransit(x =>
{
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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();