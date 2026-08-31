using System.ComponentModel.DataAnnotations;

namespace wallet.Models;

public class Wallet
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AccountNumber { get; set; } = string.Empty;
    public int CachedBalance { get; set; } = string.Empty;
} 
