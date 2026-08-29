using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using auth.Models;
using auth.Data;
using Contracts.Events;
using auth.Dto;
using MassTransit;
using auth.Events;

namespace auth.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController: Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuthController> _logger;
    private readonly IPublishEndpoint _publish;
    private readonly PasswordHasher<Auth> _hasher = new();

    public AuthController(ApplicationDbContext context, ILogger<AuthController> logger, IPublishEndpoint publish)
    {
        _context = context;
        _logger = logger;
        _publish = publish;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _context.Auths.ToListAsync();
        return Ok(users);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var exists = await _context.Auths
            .AnyAsync(a => a.Email == request.Email);

        if (exists)
            return Conflict(new { message = "Email already registered." });

        var user = new Auth
        {
            Email = request.Email,
            // FirstName = request.FirstName,
            // Lastname = request.Lastname,
            // PhoneNumber = request.PhoneNumber
        };

        user.Password = _hasher.HashPassword(user, request.Password);

        _context.Auths.Add(user);
        await _context.SaveChangesAsync();

        // a message broker should be used here and send the {first name, last name and email}
        await _publish.Publish(new UserRegistered
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber
        });

        _logger.LogInformation("Registered new user {Email}", user.Email);

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            new { user.Id, user.Email });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _context.Auths
            .FirstOrDefaultAsync(a => a.Email == request.Email);

        if (user is null)
            return Unauthorized(new { message = "Invalid email or password." });

        var result = _hasher.VerifyHashedPassword(user, user.Password, request.Password);

        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Invalid email or password." });

        await _publish.Publish(new LoginSuccessful {
            Email = request.Email
        });

        return Ok(new { message = "Login successful" });
        // (token generation goes here next)
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _context.Auths.FindAsync(id);
        if (user is null) return NotFound();
        return Ok(new { user.Id, user.Email});
    }

}