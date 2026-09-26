using MassTransit;
using Microsoft.EntityFrameworkCore;
using wallet.Data;
using wallet.Models;
using Contracts.Events;  
using wallet.Services;

namespace wallet.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegistered>
{
    private readonly ApplicationDbContext _context;
    private readonly IAccountNumberGenerator _accountNumbers;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(ApplicationDbContext context, IAccountNumberGenerator accountNumbers, ILogger<UserRegisteredConsumer> logger)
    {
        _context = context;
        _logger = logger;
        _accountNumbers = accountNumbers;
    }

    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var evt = context.Message; 

        _logger.LogInformation("Received UserRegistered for {Email}", evt.Email);

        // build a profile row from the event data
        if (await _context.Wallets.AnyAsync(w => w.UserId == evt.UserId, context.CancellationToken))
        {
            _logger.LogInformation("Wallet already exists for user {UserId}, skipping", evt.UserId);
            return;
        }

        var wallet = new WalletModel
        {
            UserId = evt.UserId,        // the shared id linking back to auth
            AccountNumber = await _accountNumbers.GenerateAsync(context.CancellationToken),
            CachedBalance = 0m,
        };

        _context.Wallets.Add(wallet);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created wallet successfully for user {UserId}", evt.UserId);
    }
}