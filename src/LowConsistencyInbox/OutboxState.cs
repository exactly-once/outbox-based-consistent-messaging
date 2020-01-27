using System;
using NServiceBus.Outbox;

public class OutboxState
{
    public Guid ClaimId { get; set; }
    public TransportOperation[] OutgoingMessages { get; set; }
}