using Domain.Entities;

namespace Domain.Scheduling;

public sealed record LeveledTask(int TaskId, int LeveledStart, int LeveledFinish, int Delay);

public sealed record LevelingResult(int ProjectDuration, IReadOnlyList<LeveledTask> LeveledTasks);

public static class ResourceLeveler
{
    public static LevelingResult Level(
        IReadOnlyList<ScheduleTask> tasks,
        IReadOnlyList<Dependency> dependencies,
        IReadOnlyList<Assignment> assignments,
        IReadOnlyDictionary<int, Resource> resourcesById)
    {
        var tasksById = tasks.ToDictionary(t => t.Id);
        var predecessorsBySuccessor = dependencies.ToLookup(d => d.SuccessorId);
        var assignmentsByTask = assignments.ToLookup(a => a.TaskId);
        var dailyUsage = new Dictionary<(int ResourceId, int Day), int>();
        var leveled = new Dictionary<int, LeveledTask>();
        var remaining = new HashSet<int>(tasks.Select(t => t.Id));
        var scheduled = new HashSet<int>();

        while (remaining.Count > 0)
        {
            var task = remaining
                .Where(id => predecessorsBySuccessor[id].All(d => scheduled.Contains(d.PredecessorId)))
                .Select(id => tasksById[id])
                .OrderBy(t => t.TotalFloat)
                .ThenBy(t => t.EarlyStart)
                .ThenBy(t => t.Id)
                .First();

            var floor = predecessorsBySuccessor[task.Id]
                .Select(d => DependencyFloor(d, leveled[d.PredecessorId], task.Duration))
                .DefaultIfEmpty(0)
                .Max();

            var taskAssignments = assignmentsByTask[task.Id].ToList();
            var start = floor;
            while (!HasCapacity(start, task.Duration, taskAssignments, resourcesById, dailyUsage))
            {
                start++;
            }

            Commit(start, task.Duration, taskAssignments, dailyUsage);
            leveled[task.Id] = new LeveledTask(task.Id, start, start + task.Duration, start - task.EarlyStart);
            remaining.Remove(task.Id);
            scheduled.Add(task.Id);
        }

        var projectDuration = leveled.Count == 0 ? 0 : leveled.Values.Max(t => t.LeveledFinish);
        return new LevelingResult(projectDuration, leveled.Values.OrderBy(t => t.TaskId).ToList());
    }

    private static int DependencyFloor(Dependency dependency, LeveledTask predecessor, int successorDuration) => dependency.Type switch
    {
        DependencyType.FinishToStart => predecessor.LeveledFinish + dependency.LagDays,
        DependencyType.StartToStart => predecessor.LeveledStart + dependency.LagDays,
        DependencyType.FinishToFinish => predecessor.LeveledFinish + dependency.LagDays - successorDuration,
        DependencyType.StartToFinish => predecessor.LeveledStart + dependency.LagDays - successorDuration,
        _ => throw new ArgumentOutOfRangeException(nameof(dependency)),
    };

    private static bool HasCapacity(
        int start, int duration, IReadOnlyList<Assignment> taskAssignments,
        IReadOnlyDictionary<int, Resource> resourcesById,
        Dictionary<(int ResourceId, int Day), int> dailyUsage)
    {
        foreach (var assignment in taskAssignments)
        {
            var capacity = resourcesById[assignment.ResourceId].MaxUnitsPerDay;
            for (var day = start; day < start + duration; day++)
            {
                if (dailyUsage.GetValueOrDefault((assignment.ResourceId, day)) + assignment.Units > capacity)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static void Commit(
        int start, int duration, IReadOnlyList<Assignment> taskAssignments,
        Dictionary<(int ResourceId, int Day), int> dailyUsage)
    {
        foreach (var assignment in taskAssignments)
        {
            for (var day = start; day < start + duration; day++)
            {
                var key = (assignment.ResourceId, day);
                dailyUsage[key] = dailyUsage.GetValueOrDefault(key) + assignment.Units;
            }
        }
    }
}