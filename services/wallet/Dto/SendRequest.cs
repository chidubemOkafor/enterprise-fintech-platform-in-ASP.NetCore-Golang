using System.ComponentModel.DataAnnotations;

namespace wallet.Dto;

public class SendRequest {
    [Required]
    public string AccountNumber { get; set; } = string.Empty;

    [Required]
    public int Amount { get; set; }

}