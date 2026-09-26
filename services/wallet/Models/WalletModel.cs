using System.ComponentModel.DataAnnotations;

namespace wallet.Models;

public class WalletModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal CachedBalance { get; set; }
} 
