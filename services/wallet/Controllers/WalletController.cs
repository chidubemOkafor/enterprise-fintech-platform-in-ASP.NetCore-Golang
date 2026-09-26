using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using wallet.Data;
using MassTransit;
using wallet.Dto;
using Contracts.Grpc;
using Contracts.Events;
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
            _logger.LogWarning("Wallet {WalletId} has no account record for UserId {UserId}",
                recipientWallet.Id, recipientWallet.UserId);
            return NotFound(new { message = "Recipient has no account record" });
        }
        catch (RpcException ex) when (ex.StatusCode == Grpc.Core.StatusCode.DeadlineExceeded
                                   || ex.StatusCode == Grpc.Core.StatusCode.Unavailable)
        {
       
            _logger.LogError(ex, "Account service unreachable during send");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Account service unavailable, please retry" });
        }

        var transactionId = Guid.NewGuid();

        await using var tx = await _context.Database.BeginTransactionAsync(HttpContext.RequestAborted);

        // Lock both rows in account-number order so two opposite transfers can never
        // grab them in a different order and deadlock.
        await _context.Database
            .SqlQuery<int>($@"
                SELECT 1 AS ""Value"" FROM ""Wallets""
                WHERE ""AccountNumber"" IN ({senderWallet.AccountNumber}, {recipientWallet.AccountNumber})
                ORDER BY ""AccountNumber""
                FOR UPDATE")
            .ToListAsync(HttpContext.RequestAborted);

        // The credit only fires if the debit found a row with enough money, so the
        // two legs cannot come apart.
        var senderBalances = await _context.Database
            .SqlQuery<decimal>($@"
                WITH debit AS (
                    UPDATE ""Wallets""
                    SET ""CachedBalance"" = ""CachedBalance"" - {request.Amount}
                    WHERE ""AccountNumber"" = {senderWallet.AccountNumber}
                      AND ""CachedBalance"" >= {request.Amount}
                    RETURNING ""CachedBalance""
                ), credit AS (
                    UPDATE ""Wallets""
                    SET ""CachedBalance"" = ""CachedBalance"" + {request.Amount}
                    WHERE ""AccountNumber"" = {recipientWallet.AccountNumber}
                      AND EXISTS (SELECT 1 FROM debit)
                    RETURNING ""CachedBalance""
                )
                SELECT ""CachedBalance"" AS ""Value"" FROM debit")
            .ToListAsync(HttpContext.RequestAborted);

        if (senderBalances.Count == 0)
            return BadRequest(new { message = "Insufficient funds" });

        await _publish.Publish(new TransferCompleted
        {
            TransactionId = transactionId,
            FromAccountNumber = senderWallet.AccountNumber,
            ToAccountNumber = recipientWallet.AccountNumber,
            Amount = request.Amount,
            OccurredAt = DateTime.UtcNow
        }, HttpContext.RequestAborted);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        await tx.CommitAsync(HttpContext.RequestAborted);

        _logger.LogInformation("Transfer {TransactionId}: {Amount} from {From} to {To}",
            transactionId, request.Amount, senderWallet.AccountNumber, recipientWallet.AccountNumber);

        return Ok(new
        {
            transactionId,
            recipient = new
            {
                accountNumber = recipientWallet.AccountNumber,
                name = $"{recipient.FirstName} {recipient.LastName}".Trim(),
                email = recipient.Email
            },
            amount = request.Amount,
            balance = senderBalances[0]
        });
    }

    [HttpPost("mint")]
    public async Task<IActionResult> Mint(MintRequest req, [FromHeader(Name = "X-User-ID")] string userId)
    {
        if (!int.TryParse(userId, out var callerUserId))
            return BadRequest(new { message = "X-User-ID header must be an integer" });

        var transactionId = Guid.NewGuid();

        await using var tx = await _context.Database.BeginTransactionAsync(HttpContext.RequestAborted);

        // One statement: the add and the read of the new value happen under the same
        // row lock, so concurrent mints accumulate instead of overwriting each other.
        var balances = await _context.Database
            .SqlQuery<decimal>($@"
                UPDATE ""Wallets""
                SET ""CachedBalance"" = ""CachedBalance"" + {req.Amount}
                WHERE ""AccountNumber"" = {req.AccountNumber}
                RETURNING ""CachedBalance"" AS ""Value""")
            .ToListAsync(HttpContext.RequestAborted);

        if (balances.Count == 0)
            return NotFound(new { message = "AccountNumber does not exist" });

        await _publish.Publish(new FundsMinted
        {
            TransactionId = transactionId,
            AccountNumber = req.AccountNumber,
            Amount = req.Amount,
            MintedByUserId = callerUserId,
            OccurredAt = DateTime.UtcNow
        }, HttpContext.RequestAborted);

        await _context.SaveChangesAsync(HttpContext.RequestAborted);
        await tx.CommitAsync(HttpContext.RequestAborted);

        _logger.LogInformation("User {CallerUserId} minted {Amount} into {AccountNumber}, new balance {Balance}",
            callerUserId, req.Amount, req.AccountNumber, balances[0]);

        return Ok(new { transactionId, accountNumber = req.AccountNumber, balance = balances[0] });
    }

}
