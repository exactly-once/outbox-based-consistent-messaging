using System;

public class Transaction
{
    public Guid ClaimId { get; set; }
    public string OutboxId { get; set; }
    public bool Processed { get; set; }
}