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
}