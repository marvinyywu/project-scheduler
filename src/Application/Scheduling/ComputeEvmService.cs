using Domain.Cost;

namespace Application.Scheduling;

public sealed class ComputeEvmService(ISchedulingUnitOfWork unitOfWork)
{
    public async Task<ComputeEvmResult> ComputeAsync(int projectId, int asOfDay, CancellationToken cancellationToken = default)
    {
        var project = await unitOfWork.FindProjectAsync(projectId, cancellationToken);
        if (project is null)
        {
            return ComputeEvmResult.ProjectNotFound();
        }

        var tasks = await unitOfWork.GetTasksForProjectAsync(projectId, cancellationToken);
        return ComputeEvmResult.Success(EvmCalculator.Compute(tasks, asOfDay));
    }
}