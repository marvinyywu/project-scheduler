using Domain.Entities;

namespace Domain.Scheduling;

public static class CpmEngine
{
    private const int NegativeInfinity = int.MinValue / 2;
    private const int PositiveInfinity = int.MaxValue / 2;

    public static int Compute(IReadOnlyList<ScheduleTask> tasks, IReadOnlyList<Dependency> dependencies)
    {
        if (CycleDetector.HasCycle(tasks, dependencies))
        {
            throw new ScheduleCycleException();
        }

        var order = TopologicalSort.Sort(tasks, dependencies);
        var tasksById = tasks.ToDictionary(t => t.Id);
        var predecessorEdges = dependencies.ToLookup(d => d.SuccessorId);
        var successorEdges = dependencies.ToLookup(d => d.PredecessorId);

        RunForwardPass(order, tasksById, predecessorEdges);
        var projectDuration = tasks.Count == 0 ? 0 : tasks.Max(t => t.EarlyFinish);
        RunBackwardPass(order, tasksById, successorEdges, projectDuration);

        foreach (var task in tasks)
        {
            task.TotalFloat = task.LateStart - task.EarlyStart;
            task.IsCritical = task.TotalFloat == 0;
            task.FreeFloat = ComputeFreeFloat(task, successorEdges[task.Id], tasksById, projectDuration);
        }

        return projectDuration;
    }

    private static void RunForwardPass(
        IReadOnlyList<ScheduleTask> order,
        Dictionary<int, ScheduleTask> tasksById,
        ILookup<int, Dependency> predecessorEdges)
    {
        foreach (var task in order)
        {
            var esFromFsSs = 0;
            var efFromFfSf = NegativeInfinity;

            foreach (var dependency in predecessorEdges[task.Id])
            {
                var predecessor = tasksById[dependency.PredecessorId];
                switch (dependency.Type)
                {
                    case DependencyType.FinishToStart:
                        esFromFsSs = Math.Max(esFromFsSs, predecessor.EarlyFinish + dependency.LagDays);
                        break;
                    case DependencyType.StartToStart:
                        esFromFsSs = Math.Max(esFromFsSs, predecessor.EarlyStart + dependency.LagDays);
                        break;
                    case DependencyType.FinishToFinish:
                        efFromFfSf = Math.Max(efFromFfSf, predecessor.EarlyFinish + dependency.LagDays);
                        break;
                    case DependencyType.StartToFinish:
                        efFromFfSf = Math.Max(efFromFfSf, predecessor.EarlyStart + dependency.LagDays);
                        break;
                }
            }

            task.EarlyStart = efFromFfSf == NegativeInfinity
                ? esFromFsSs
                : Math.Max(esFromFsSs, efFromFfSf - task.Duration);
            task.EarlyFinish = task.EarlyStart + task.Duration;
        }
    }

    private static void RunBackwardPass(
        IReadOnlyList<ScheduleTask> order,
        Dictionary<int, ScheduleTask> tasksById,
        ILookup<int, Dependency> successorEdges,
        int projectDuration)
    {
        for (var i = order.Count - 1; i >= 0; i--)
        {
            var task = order[i];
            var successors = successorEdges[task.Id];

            if (!successors.Any())
            {
                task.LateFinish = projectDuration;
                task.LateStart = task.LateFinish - task.Duration;
                continue;
            }

            var lfFromFsFf = PositiveInfinity;
            var lsFromSsSf = PositiveInfinity;

            foreach (var dependency in successors)
            {
                var successor = tasksById[dependency.SuccessorId];
                switch (dependency.Type)
                {
                    case DependencyType.FinishToStart:
                        lfFromFsFf = Math.Min(lfFromFsFf, successor.LateStart - dependency.LagDays);
                        break;
                    case DependencyType.StartToStart:
                        lsFromSsSf = Math.Min(lsFromSsSf, successor.LateStart - dependency.LagDays);
                        break;
                    case DependencyType.FinishToFinish:
                        lfFromFsFf = Math.Min(lfFromFsFf, successor.LateFinish - dependency.LagDays);
                        break;
                    case DependencyType.StartToFinish:
                        lsFromSsSf = Math.Min(lsFromSsSf, successor.LateFinish - dependency.LagDays);
                        break;
                }
            }

            task.LateStart = lfFromFsFf == PositiveInfinity
                ? lsFromSsSf
                : Math.Min(lsFromSsSf, lfFromFsFf - task.Duration);
            task.LateFinish = task.LateStart + task.Duration;
        }
    }

    private static int ComputeFreeFloat(
        ScheduleTask task,
        IEnumerable<Dependency> successors,
        Dictionary<int, ScheduleTask> tasksById,
        int projectDuration)
    {
        var successorList = successors.ToList();
        if (successorList.Count == 0)
        {
            return projectDuration - task.EarlyFinish;
        }

        var freeFloat = PositiveInfinity;
        foreach (var dependency in successorList)
        {
            var successor = tasksById[dependency.SuccessorId];
            var slack = dependency.Type switch
            {
                DependencyType.FinishToStart => successor.EarlyStart - dependency.LagDays - task.EarlyFinish,
                DependencyType.StartToStart => successor.EarlyStart - dependency.LagDays - task.EarlyStart,
                DependencyType.FinishToFinish => successor.EarlyFinish - dependency.LagDays - task.EarlyFinish,
                DependencyType.StartToFinish => successor.EarlyFinish - dependency.LagDays - task.EarlyStart,
                _ => PositiveInfinity
            };
            freeFloat = Math.Min(freeFloat, slack);
        }

        return freeFloat;
    }
}
