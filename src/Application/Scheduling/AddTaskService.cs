using Domain.Entities;

namespace Application.Scheduling;

public sealed class AddTaskService(ISchedulingUnitOfWork unitOfWork)
{
    public async Task<AddTaskResult> AddAsync(int projectId, string name, int duration, decimal budget = 0m, CancellationToken cancellationToken = default)
    {
        var project = await unitOfWork.FindProjectAsync(projectId, cancellationToken);
        if (project is null)
        {
            return AddTaskResult.ProjectNotFound();
        }

        var task = new ScheduleTask
        {
            Name = name,
            Duration = duration,
            ProjectId = projectId,
            Budget = budget
        };

        unitOfWork.AddTask(task);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return AddTaskResult.Success(task);
    }
}
