using System.ComponentModel.DataAnnotations;

namespace wallet.Dto;

public class MintRequest {
    [Required]
    public string AccountNumber { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

}
