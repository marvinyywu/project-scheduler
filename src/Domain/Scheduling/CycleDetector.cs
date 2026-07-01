using Domain.Entities;

namespace Domain.Scheduling;

public static class CycleDetector
{
    private enum VisitState { Unvisited, Visiting, Visited }

    public static bool HasCycle(IReadOnlyCollection<ScheduleTask> tasks, IReadOnlyCollection<Dependency> dependencies)
    {
        var successors = tasks.ToDictionary(t => t.Id, _ => new List<int>());
        foreach (var dependency in dependencies)
        {
            successors[dependency.PredecessorId].Add(dependency.SuccessorId);
        }

        var state = tasks.ToDictionary(t => t.Id, _ => VisitState.Unvisited);

        foreach (var task in tasks)
        {
            if (state[task.Id] == VisitState.Unvisited && HasCycleFrom(task.Id, successors, state))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasCycleFrom(int taskId, Dictionary<int, List<int>> successors, Dictionary<int, VisitState> state)
    {
        state[taskId] = VisitState.Visiting;

        foreach (var successorId in successors[taskId])
        {
            if (state[successorId] == VisitState.Visiting)
            {
                return true;
            }

            if (state[successorId] == VisitState.Unvisited && HasCycleFrom(successorId, successors, state))
            {
                return true;
            }
        }

        state[taskId] = VisitState.Visited;
        return false;
    }
}
