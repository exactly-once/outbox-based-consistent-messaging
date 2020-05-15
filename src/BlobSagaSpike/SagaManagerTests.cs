using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Xml;
using NServiceBus;
using NServiceBus.Extensibility;
using NUnit.Framework;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

[TestFixture]
public class SagaManagerTests
{
    /*
    [Test]
    public async Task PerformScenarios()
    {
        var scenarios = GenerateScenarios(10).ToArray();

        foreach (var scenario in scenarios)
        {
            await PerformScenario(scenario);
        }
    }

    public async Task PerformScenario(string scenario)
    {
        var controller = new TestController(scenario);
        var persister = new InMemorySagaPersister();
        var inbox = new InMemoryInbox();
        var dispatcher = new FakeDispatcher();
        var persisterA = new TestingSagaDataPersister(persister, inbox, 'A', controller.GetBarrier);
        var persisterB = new TestingSagaDataPersister(persister, inbox, 'B', controller.GetBarrier);

        var managerA = new SagaManager(persisterA, persisterA, dispatcher);
        var managerB = new SagaManager(persisterB, persisterB, dispatcher);

        var processA = Task.Run(() => ProcessMessage(managerA, controller));
        var processB = Task.Run(() => ProcessMessage(managerB, controller));

        var done = Task.WhenAll(processA, processB);
        await done.ConfigureAwait(false);

        var dataContainer = await persister.LoadByCorrelationId("correlationId");
        var sagaData = (SagaData)dataContainer.SagaData;

        Assert.AreEqual(1, sagaData.Counter);
    }

    IEnumerable<string> GenerateScenarios(int remaining)
    {
        if (remaining == 1)
        {
            yield return "A";
            yield return "B";
        }
        else
        {
            foreach (var subscenario in GenerateScenarios(remaining - 1))
            {
                yield return "A" + subscenario;
                yield return "B" + subscenario;
            }
        }
    }

    async Task ProcessMessage(SagaManager managerA, TestController testController)
    {
        var completed = false;
        while (!completed)
        {
            try
            {
                await managerA.Process<SagaData>("messageId", "correlationId", new ContextBag(), HandlerCallback);
                completed = true;
                testController.Complete();
            }
            catch (ConcurrencyException e)
            {
                //Swallow and retry
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    [Test]
    public async Task Try()
    {
        var persister = new InMemorySagaPersister();
        var inbox = new InMemoryInbox();
        var dispatcher = new FakeDispatcher();
        var manager = new SagaManager(persister, inbox, dispatcher);

        await manager.Process<SagaData>("messageId", "correlationId", new ContextBag(),
            HandlerCallback);

        var dataContainer = await persister.LoadByCorrelationId("correlationId");
        var sagaData = (SagaData) dataContainer.SagaData;

        Assert.AreEqual(1, sagaData.Counter);
    }

    Task<(SagaData, PendingTransportOperations)> HandlerCallback(SagaData data, ContextBag context)
    {
        data.Counter++;
        return Task.FromResult<(SagaData, PendingTransportOperations)>((data, new PendingTransportOperations()));
    }

    class SagaData
    {
        public int Counter { get; set; }
    }
    */
}