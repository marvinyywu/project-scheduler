using Domain.Scheduling;

namespace Application.Scheduling;

public sealed class LevelScheduleService(ISchedulingUnitOfWork unitOfWork)
{
    public async Task<LevelScheduleResult> LevelAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var project = await unitOfWork.FindProjectAsync(projectId, cancellationToken);
        if (project is null)
        {
            return LevelScheduleResult.ProjectNotFound();
        }

        var tasks = await unitOfWork.GetTasksForProjectAsync(projectId, cancellationToken);
        var taskIds = tasks.Select(t => t.Id).ToHashSet();
        var dependencies = await unitOfWork.GetDependenciesForTasksAsync(taskIds, cancellationToken);
        var assignments = await unitOfWork.GetAssignmentsForTasksAsync(taskIds, cancellationToken);
        var resourceIds = assignments.Select(a => a.ResourceId).Distinct().ToList();
        var resources = await unitOfWork.GetResourcesByIdsAsync(resourceIds, cancellationToken);

        var originalDuration = tasks.Count == 0 ? 0 : tasks.Max(t => t.EarlyFinish);
        var leveled = ResourceLeveler.Level(tasks, dependencies, assignments, resources.ToDictionary(r => r.Id));

        return LevelScheduleResult.Success(originalDuration, leveled.ProjectDuration, leveled.LeveledTasks);
    }
}
