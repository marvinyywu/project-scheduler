using Domain.Entities;

namespace Domain.Scheduling;

public static class TopologicalSort
{
    public static IReadOnlyList<ScheduleTask> Sort(IReadOnlyList<ScheduleTask> tasks, IReadOnlyList<Dependency> dependencies)
    {
        var inDegree = tasks.ToDictionary(t => t.Id, _ => 0);
        var successors = tasks.ToDictionary(t => t.Id, _ => new List<int>());

        foreach (var dependency in dependencies)
        {
            successors[dependency.PredecessorId].Add(dependency.SuccessorId);
            inDegree[dependency.SuccessorId]++;
        }

        var tasksById = tasks.ToDictionary(t => t.Id);
        var queue = new Queue<int>(tasks.Where(t => inDegree[t.Id] == 0).Select(t => t.Id));
        var ordered = new List<ScheduleTask>(tasks.Count);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            ordered.Add(tasksById[id]);

            foreach (var successorId in successors[id])
            {
                if (--inDegree[successorId] == 0)
                {
                    queue.Enqueue(successorId);
                }
            }
        }

        if (ordered.Count != tasks.Count)
        {
            throw new ScheduleCycleException();
        }

        return ordered;
    }
}
