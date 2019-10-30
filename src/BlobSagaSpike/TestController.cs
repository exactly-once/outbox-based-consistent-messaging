using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

class TestController
{
    List<StepBarrier> scenario;
    int index;
    bool completed;

    public TestController(string scenario)
    {
        this.scenario = scenario.Select(x => new StepBarrier(x)).ToList();
    }

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

    public Task GetBarrier(char threadId, bool starting)
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

            if (!starting)
            {
                //If a thread is already running and is going to wait on a barrier, it unlock the next barrier
                index++;
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