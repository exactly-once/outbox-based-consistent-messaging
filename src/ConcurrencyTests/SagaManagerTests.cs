using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NUnit.Framework;

[TestFixture]
public class SagaManagerTests
{
    const string SagaId = "correlationId";
    const string MessageId = "messageId";

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
        Console.WriteLine("Scenario: " + scenario);

        var dispatcher = new FakeDispatcher();
        //var sagaManagerFactory = new BaselineSagaManagerFactory();
        //var sagaManagerFactory = new BasicInboxSagaManagerFactory();
        var sagaManagerFactory = new InboxWithOutOfDocumentOutboxSagaManagerFactory();
        
        var controller = new TestController(scenario);

        var managerA = sagaManagerFactory.Create(s => controller.GetBarrier('A', s), dispatcher);
        var managerB = sagaManagerFactory.Create(s => controller.GetBarrier('B', s), dispatcher);

        var processA = Task.Run(() => ProcessMessage(managerA, controller));
        var processB = Task.Run(() => ProcessMessage(managerB, controller));

        var done = Task.WhenAll(processA, processB);
        await done.ConfigureAwait(false);

        var sagaData = await sagaManagerFactory.LoadSaga(SagaId);

        Assert.AreEqual(1, ((SagaData)sagaData).Counter);

        
        foreach (var call in controller.CallHistory)
        {
            Console.WriteLine(" - " + call);
        }
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

    async Task ProcessMessage(ISagaManager managerA, TestController testController)
    {
        var completed = false;
        while (!completed)
        {
            try
            {
                await managerA.Process<SagaData>(MessageId, SagaId, new ContextBag(), HandlerCallback);
                completed = true;
                testController.Complete();
            }
            catch (ScenarioIncompleteException e)
            {
                Console.WriteLine("Scenario incomplete");
                break;
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

    Task<(SagaData, PendingTransportOperations)> HandlerCallback(SagaData data, ContextBag context)
    {
        data.Counter++;
        return Task.FromResult<(SagaData, PendingTransportOperations)>((data, new PendingTransportOperations()));
    }

    class SagaData
    {
        public int Counter { get; set; }
    }
}