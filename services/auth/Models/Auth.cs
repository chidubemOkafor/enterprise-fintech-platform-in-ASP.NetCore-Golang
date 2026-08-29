using System.ComponentModel.DataAnnotations;

namespace auth.Models;

public class Auth
{
    public int Id { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

}

// dotnet ef migrations add InitialCreate
// dotnet ef database update