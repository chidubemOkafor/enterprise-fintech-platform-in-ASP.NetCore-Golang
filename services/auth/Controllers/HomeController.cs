using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using auth.Models;
using auth.Data;

namespace auth.Controllers;

public class HomeController : Controller
{

    private readonly ApplicationDbContext _context;

    // The framework automatically injects the DB context here
    public ProductController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        // Fetches all items from the Products table using the ORM
        var products = await _context.Products.ToListAsync();
        return View(products); 
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
