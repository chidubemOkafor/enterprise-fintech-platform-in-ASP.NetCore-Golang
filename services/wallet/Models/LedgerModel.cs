using System.ComponentModel.DataAnnotations;

namespace wallet.Models;

public class Ledger
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CachedBalance { get; set; } = string.Empty;
} 
