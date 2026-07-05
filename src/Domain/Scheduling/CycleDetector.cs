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

    // Iterative DFS with an explicit heap-allocated stack, not recursion: a long
    // dependency chain (thousands of tasks each depending on the previous one)
    // would otherwise recurse once per task and overflow the call stack, which
    // is a fatal, uncatchable crash in .NET, not a normal exception.
    private static bool HasCycleFrom(int startId, Dictionary<int, List<int>> successors, Dictionary<int, VisitState> state)
    {
        var stack = new Stack<(int TaskId, int NextSuccessorIndex)>();
        stack.Push((startId, 0));
        state[startId] = VisitState.Visiting;

        while (stack.Count > 0)
        {
            var (taskId, nextSuccessorIndex) = stack.Pop();
            var taskSuccessors = successors[taskId];

            if (nextSuccessorIndex >= taskSuccessors.Count)
            {
                state[taskId] = VisitState.Visited;
                continue;
            }

            stack.Push((taskId, nextSuccessorIndex + 1));

            var successorId = taskSuccessors[nextSuccessorIndex];
            if (state[successorId] == VisitState.Visiting)
            {
                return true;
            }

            if (state[successorId] == VisitState.Unvisited)
            {
                state[successorId] = VisitState.Visiting;
                stack.Push((successorId, 0));
            }
        }

        return false;
    }
}
