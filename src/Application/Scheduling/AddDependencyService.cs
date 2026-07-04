using Domain.Entities;
using Domain.Scheduling;

namespace Application.Scheduling;

public sealed class AddDependencyService(ISchedulingUnitOfWork unitOfWork)
{
    public async Task<AddDependencyResult> AddAsync(
        int predecessorId,
        int successorId,
        DependencyType type,
        int lagDays,
        CancellationToken cancellationToken = default)
    {
        var predecessor = await unitOfWork.FindTaskAsync(predecessorId, cancellationToken);
        var successor = await unitOfWork.FindTaskAsync(successorId, cancellationToken);
        if (predecessor is null || successor is null)
        {
            return AddDependencyResult.TaskNotFound();
        }

        var tasks = await unitOfWork.GetTasksForProjectAsync(predecessor.ProjectId, cancellationToken);
        var taskIds = tasks.Select(t => t.Id).ToHashSet();
        var dependencies = await unitOfWork.GetDependenciesForTasksAsync(taskIds, cancellationToken);

        var newDependency = new Dependency
        {
            PredecessorId = predecessorId,
            SuccessorId = successorId,
            Type = type,
            LagDays = lagDays
        };

        // Stage the dependency and recompute against it before saving, so a
        // cycle is caught and discarded here rather than ever hitting the DB.
        unitOfWork.AddDependency(newDependency);
        dependencies.Add(newDependency);

        try
        {
            CpmEngine.Compute(tasks, dependencies);
        }
        catch (ScheduleCycleException)
        {
            unitOfWork.RemoveDependency(newDependency);
            return AddDependencyResult.CycleDetected();
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AddDependencyResult.Success(newDependency);
    }
}
