namespace Application.Scheduling;

public sealed class UpdateTaskProgressService(ISchedulingUnitOfWork unitOfWork)
{
    public async Task<UpdateTaskProgressResult> UpdateAsync(
        int taskId, double percentComplete, decimal actualCost, CancellationToken cancellationToken = default)
    {
        var task = await unitOfWork.FindTaskAsync(taskId, cancellationToken);
        if (task is null)
        {
            return UpdateTaskProgressResult.TaskNotFound();
        }

        task.PercentComplete = percentComplete;
        task.ActualCost = actualCost;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return UpdateTaskProgressResult.Success(task);
    }
}