using System.ComponentModel.DataAnnotations;

namespace auth.Models;

public class Auth
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

}

// dotnet ef migrations add InitialCreate
// dotnet ef database update