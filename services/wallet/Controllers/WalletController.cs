using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace wallet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController: Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthController> _logger;
    private readonly IPublishEndpoint _publish;
}