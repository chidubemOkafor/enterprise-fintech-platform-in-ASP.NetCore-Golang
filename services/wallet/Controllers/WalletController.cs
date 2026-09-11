using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using wallet.Data;
using MassTransit;
using wallet.Dto;

namespace wallet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController: Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WalletController> _logger;
    private readonly IPublishEndpoint _publish;

    public WalletController (ApplicationDbContext context, ILogger<WalletController> logger, IPublishEndpoint publish)
    {
        _context = context;
        _logger = logger;
        _publish = publish;
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
        var wallet = await _context.Wallets.FindAsync(request.AccountNumber);
        if (wallet is null) return NotFound(new { message = "AccountNumber does not exist"});

        // Now you have userId available to use
        // i need a way to get the user information via grpc from the account service
    }

}