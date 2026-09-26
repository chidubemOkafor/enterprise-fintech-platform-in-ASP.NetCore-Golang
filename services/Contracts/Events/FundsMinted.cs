namespace Contracts.Events;

public class FundsMinted
{
    public Guid TransactionId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int MintedByUserId { get; set; }
    public DateTime OccurredAt { get; set; }
}
