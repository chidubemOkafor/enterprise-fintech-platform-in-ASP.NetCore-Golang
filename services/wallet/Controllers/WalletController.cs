using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using wallet.Data;
using MassTransit;

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
}