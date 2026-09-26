using account.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using account.Models;
using MassTransit;

namespace account.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController: Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;
    private readonly IPublishEndpoint _publish;

    public AccountController(ApplicationDbContext context, ILogger<AccountController> logger, IPublishEndpoint publish)
    {
        _context = context;
        _logger = logger;
        _publish = publish;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Accounts.ToListAsync();
        return Ok(users);
    }


    [HttpGet("me")]
    public async Task<IActionResult> GetUser([FromHeader(Name = "X-User-ID")] string userId)
    {
        if (!int.TryParse(userId, out var id))
            return BadRequest(new { message = "X-User-ID header must be an integer" });

        var user = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == id);

        if (user is null)
            return NotFound(new { message = "User not found" });
        
        // the account should return 
        // 1. the account number 
        // 2. 
        return Ok(user);
    }

}