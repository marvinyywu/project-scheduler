using Domain.Scheduling;

namespace Application.Scheduling;

public sealed class RecomputeScheduleService(ISchedulingUnitOfWork unitOfWork)
{
    public async Task<RecomputeResult> RecomputeAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var project = await unitOfWork.FindProjectAsync(projectId, cancellationToken);
        if (project is null)
        {
            return RecomputeResult.ProjectNotFound();
        }

        var tasks = await unitOfWork.GetTasksForProjectAsync(projectId, cancellationToken);
        var taskIds = tasks.Select(t => t.Id).ToHashSet();
        var dependencies = await unitOfWork.GetDependenciesForTasksAsync(taskIds, cancellationToken);

        int projectDuration;
        try
        {
            // Tasks/dependencies here are the same EF-tracked instances the engine
            // mutates in place, so SaveChangesAsync persists ES/EF/LS/LF/float
            // without any manual mapping back from the engine's output.
            projectDuration = CpmEngine.Compute(tasks, dependencies);
        }
        catch (ScheduleCycleException)
        {
            return RecomputeResult.CycleDetected();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RecomputeResult.Success(projectDuration);
    }
}
