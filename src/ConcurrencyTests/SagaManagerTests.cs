using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Extensibility;
using NServiceBus.Transport;
using NUnit.Framework;

[TestFixture]
public class SagaManagerTests
{
    const string SagaId = "correlationId";
    const string MessageId = "messageId";

    [Test]
    public async Task PerformScenarios()
    {
        var scenarios = GenerateScenarios(11).ToArray();
        //var scenarios = new[] { "ABABABABABA" };

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
        //var sagaManagerFactory = new InboxWithOutOfDocumentOutboxSagaManagerFactory();
        var sagaManagerFactory = new TokenBasedWithExternalOutboxSagaManagerFactory();
        sagaManagerFactory.PrepareMessage(MessageId);
        
        var controller = new TestController(scenario);

        var processes = new Dictionary<char, SagaManagerTask>
        {
            ['A'] = new SagaManagerTask(sagaManagerFactory, dispatcher, MessageId, SagaId, "A"),
            ['B'] = new SagaManagerTask(sagaManagerFactory, dispatcher, MessageId, SagaId, "B")
        };

        foreach (var process in scenario)
        {
            await processes[process].MakeStep();
        }

        var sagaData = await sagaManagerFactory.LoadSaga(SagaId);
        if (sagaData != null)
        {
            Assert.AreEqual(1, ((SagaData) sagaData).Counter);
        }

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
}

class SagaManagerTask
{
    ISagaManager manager;
    string messageId;
    string sagaId;
    string processId;
    Task processingTask;
    TaskCompletionSource<bool> barrier;
    TaskCompletionSource<string> stepComplete;

    public SagaManagerTask(ISagaManagerFactory managerFactory, IDispatchMessages dispatcher, string messageId, string sagaId, string processId)
    {
        this.manager = managerFactory.Create(GetBarrier, dispatcher);
        this.messageId = messageId;
        this.sagaId = sagaId;
        this.processId = processId;
    }

    Task GetBarrier(string arg)
    {
        var message = $"{processId}: {arg}";
        var myBarrier = barrier;
        stepComplete.SetResult(message); //Signal that this step is complete
        return myBarrier.Task;
    }

    public async Task MakeStep()
    {
        if (processingTask == null)
        {
            barrier = new TaskCompletionSource<bool>();
            processingTask = Task.Run(ProcessingLoop);
        }

        var oldBarrier = barrier;
        barrier = new TaskCompletionSource<bool>();

        stepComplete = new TaskCompletionSource<string>();
        
        oldBarrier.SetResult(true); //Allow process to move through next barrier

        var message = await stepComplete.Task;
        Console.WriteLine(message);
    }

    async Task ProcessingLoop()
    {
        while (true)
        {
            try
            {
                await manager.Process<SagaData>(messageId, sagaId, new ContextBag(), HandlerCallback);
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
}


public class SagaData
{
    public int Counter { get; set; }
}