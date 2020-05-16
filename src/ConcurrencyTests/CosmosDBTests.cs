using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NUnit.Framework;

namespace ConcurrencyTests
{
    public class CosmosDbTests
    {
        const string SagaId = "correlationId";
        const string MessageId = "messageId";

        [Test]
        public async Task ProcessSingleMessage()
        {
            var manager = await new CosmosDBSagaManagerFactory().Create(new FakeDispatcher());

            await manager.Process<SagaData>(MessageId, SagaId, new ContextBag(), HandlerCallback);
        }

        Task<(SagaData, PendingTransportOperations)> HandlerCallback(SagaData data, ContextBag context)
        {
            data.Counter++;

            return Task.FromResult<(SagaData, PendingTransportOperations)>((data, new PendingTransportOperations()));
        }
    }

    class SagaData : E1Content
    {
        public int Counter { get; set; }
    }
}