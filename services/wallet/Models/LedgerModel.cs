using System.ComponentModel.DataAnnotations;

namespace wallet.Models;

public class LedgerModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CachedBalance { get; set; } = string.Empty;
} 
