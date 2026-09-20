using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using wallet.Data;
using MassTransit;
using wallet.Dto;
using Contracts.Grpc;
using Grpc.Core;

namespace wallet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController: Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WalletController> _logger;
    private readonly IPublishEndpoint _publish;
    private readonly AccountService.AccountServiceClient _accountClient;

    public WalletController (
        ApplicationDbContext context,
        ILogger<WalletController> logger,
        IPublishEndpoint publish,
        AccountService.AccountServiceClient accountClient)
    {
        _context = context;
        _logger = logger;
        _publish = publish;
        _accountClient = accountClient;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var wallet = await _context.Wallets.FindAsync(id);
        if (wallet is null) return NotFound();
        return Ok(new { wallet.UserId, wallet.CachedBalance, wallet.AccountNumber});
    }

    // send money takes the destination account number and amount to send 
    // first check if the account number is a valid account numbet
    // next check if you have the amount you are trying to send


    [HttpPost]
    public async Task<IActionResult> Send(SendRequest request, [FromHeader(Name = "X-User-ID")] string userId)
    {
        if (!int.TryParse(userId, out var senderUserId))
            return BadRequest(new { message = "X-User-ID header must be an integer" });

        // AccountNumber is not the primary key, so FindAsync (which looks up by PK)
        // cannot be used here.
        var recipientWallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.AccountNumber == request.AccountNumber);
        if (recipientWallet is null)
            return NotFound(new { message = "AccountNumber does not exist"});

        var senderWallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == senderUserId);
        if (senderWallet is null)
            return NotFound(new { message = "No wallet found for the calling user" });

        if (senderWallet.Id == recipientWallet.Id)
            return BadRequest(new { message = "Cannot send to your own account" });

        // Cross-service call to the account service: confirms the recipient is a
        // real account holder and pulls their name so the caller can see who they
        // are paying before the transfer is committed.
        GetUserResponse recipient;
        try
        {
            recipient = await _accountClient.GetUserAsync(
                new GetUserRequest { UserId = recipientWallet.UserId },
                deadline: DateTime.UtcNow.AddSeconds(5),
                cancellationToken: HttpContext.RequestAborted);
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.NotFound)
        {
            // The wallet exists but the account service has no user for it.
            _logger.LogWarning("Wallet {WalletId} has no account record for UserId {UserId}",
                recipientWallet.Id, recipientWallet.UserId);
            return NotFound(new { message = "Recipient has no account record" });
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded
                                   || ex.StatusCode == Grpc.Core.StatusCode.Unavailable)
        {
            // Downstream is down or slow — surface it as 503 rather than a 500,
            // so the caller knows this is retryable.
            _logger.LogError(ex, "Account service unreachable during send");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Account service unavailable, please retry" });
        }

        // TODO: debit senderWallet, credit recipientWallet, publish the event.
        // Blocked on CachedBalance being a string — it needs to be decimal (and a
        // migration) before any arithmetic here is safe. Both writes must also
        // share one transaction.

        return Ok(new
        {
            recipient = new
            {
                accountNumber = recipientWallet.AccountNumber,
                name = $"{recipient.FirstName} {recipient.LastName}".Trim(),
                email = recipient.Email
            },
            amount = request.Amount
        });
    }

}
