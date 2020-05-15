using Newtonsoft.Json;
using NServiceBus.Outbox;

public class OutboxState
{
    [JsonProperty("id")]
    public string Id { get; set; }

    public string MessageId { get; set; }

    public TransportOperation[] OutgoingMessages { get; set; }
}