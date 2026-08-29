using MassTransit;
using account.Data;
using account.Models;
using Contracts.Events;  

namespace account.Consumers;

public class UserRegisteredConsumer : IConsumer<UserRegistered>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(ApplicationDbContext context, ILogger<UserRegisteredConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<UserRegistered> context)
    {
        var evt = context.Message;   // the event auth published

        _logger.LogInformation("Received UserRegistered for {Email}", evt.Email);

        // build a profile row from the event data
        var profile = new UserModel
        {
            UserId = evt.UserId,        // the shared id linking back to auth
            Email = evt.Email,
            FirstName = evt.FirstName,
            LastName = evt.LastName,
            PhoneNumber = evt.PhoneNumber
        };

        _context.Accounts.Add(profile);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created profile for user {UserId}", evt.UserId);
    }
}