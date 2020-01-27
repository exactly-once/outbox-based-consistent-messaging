using System.Collections.Generic;
using NServiceBus;

public class OrderSubmitted : IEvent
{
    public string CorrelationId { get; set; }
    public List<string> Items { get; set; }
}