using Domain.Entities;

namespace Application.Scheduling;

public sealed class AssignResourceService(ISchedulingUnitOfWork unitOfWork)
{
    public async Task<AssignResourceResult> AssignAsync(int taskId, int resourceId, int units, CancellationToken cancellationToken = default)
    {
        var task = await unitOfWork.FindTaskAsync(taskId, cancellationToken);
        if (task is null)
        {
            return AssignResourceResult.TaskNotFound();
        }

        var resource = await unitOfWork.FindResourceAsync(resourceId, cancellationToken);
        if (resource is null)
        {
            return AssignResourceResult.ResourceNotFound();
        }

        var assignment = new Assignment { TaskId = taskId, ResourceId = resourceId, Units = units };
        unitOfWork.AddAssignment(assignment);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AssignResourceResult.Success(assignment);
    }
}
