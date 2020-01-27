using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class ScenarioIncompleteException : Exception
{
}

class TestController
{
    List<StepBarrier> scenario;
    int index;
    bool completed;
    HashSet<char> startedThreads = new HashSet<char>();
    List<string> callHistory = new List<string>();

    public TestController(string scenario)
    {
        this.scenario = scenario.Select(x => new StepBarrier(x)).ToList();
    }

    public IEnumerable<string> CallHistory => callHistory;

    public void Complete()
    {
        lock (scenario)
        {
            completed = true;
            foreach (var step in scenario)
            {
                step.CompletionSource.TrySetResult(true);
            }
        }
    }

    public Task GetBarrier(char threadId, string call)
    {
        lock (scenario)
        {
            if (completed)
            {
                return Task.CompletedTask;
            }
            var barrierIndex = scenario.FindIndex(b => b.ThreadId == threadId);
            if (barrierIndex < 0)
            {
                return scenario[scenario.Count - 1].CompletionSource.Task;
            }
            var barrier = scenario[barrierIndex];

            if (startedThreads.Contains(threadId))
            {
                //If a thread is already running and is going to wait on a barrier, it unlock the next barrier
                index++;
                if (index >= scenario.Count)
                {
                    throw new ScenarioIncompleteException();
                }
                scenario[index].CompletionSource.SetResult(true);
            }
            else if (barrierIndex == 0)
            {
                //Otherwise if the thread is requesting the very first barrier, it automatically unlocks it
                scenario[0].CompletionSource.SetResult(true);
            }
            else if (barrierIndex == scenario.Count - 1)
            {
                //Otherwise if the thread is requesting the very last barrier, it automatically unlocks it
                scenario[barrierIndex].CompletionSource.SetResult(true);
            }

            startedThreads.Add(threadId);
            callHistory.Add($"{threadId}: {call}");
            return barrier.CompletionSource.Task;
        }
    }

    class StepBarrier
    {
        public StepBarrier(char threadId)
        {
            ThreadId = threadId;
            CompletionSource = new TaskCompletionSource<bool>();
        }

        public char ThreadId { get; }
        public TaskCompletionSource<bool> CompletionSource { get; }
    }
}