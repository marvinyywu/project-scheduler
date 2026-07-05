using System.Diagnostics;
using Domain.Entities;
using Domain.Scheduling;

const int WarmupIterations = 5;
const int Iterations = 20;
int[] sizes = [100, 1_000, 10_000];

Console.WriteLine($"{"Tasks",8} | {"p50 (ms)",10} | {"p95 (ms)",10}");

foreach (var size in sizes)
{
    var (tasks, dependencies) = BuildChain(size);
    var timings = new List<double>(Iterations);

    // Discard the first few runs: JIT tiering and GC transitions dominate a
    // cold start and would otherwise report a one-off spike as if it were
    // typical steady-state latency, which is what a long-running API process
    // actually experiences after warm-up, not what a benchmark's first
    // iteration sees.
    for (var i = 0; i < WarmupIterations + Iterations; i++)
    {
        foreach (var task in tasks)
        {
            task.EarlyStart = task.EarlyFinish = task.LateStart = task.LateFinish = 0;
        }

        var stopwatch = Stopwatch.StartNew();
        CpmEngine.Compute(tasks, dependencies);
        stopwatch.Stop();

        if (i >= WarmupIterations)
        {
            timings.Add(stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    timings.Sort();
    var p50 = timings[Iterations / 2];
    var p95 = timings[(int)(Iterations * 0.95) - 1];
    Console.WriteLine($"{size,8} | {p50,10:F3} | {p95,10:F3}");
}

static (List<ScheduleTask> tasks, List<Dependency> dependencies) BuildChain(int size)
{
    var tasks = new List<ScheduleTask>(size);
    var dependencies = new List<Dependency>(size - 1);

    for (var i = 1; i <= size; i++)
    {
        tasks.Add(new ScheduleTask { Id = i, Name = $"T{i}", Duration = 1 });
    }

    for (var i = 2; i <= size; i++)
    {
        dependencies.Add(new Dependency { Id = i - 1, PredecessorId = i - 1, SuccessorId = i });
    }

    return (tasks, dependencies);
}
