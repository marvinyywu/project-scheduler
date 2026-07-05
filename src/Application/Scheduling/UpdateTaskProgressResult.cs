using Domain.Entities;

namespace Application.Scheduling;

public enum UpdateTaskProgressOutcome
{
    Success,
    TaskNotFound
}

public sealed record UpdateTaskProgressResult(UpdateTaskProgressOutcome Outcome, ScheduleTask? Task = null)
{
    public static UpdateTaskProgressResult Success(ScheduleTask task) => new(UpdateTaskProgressOutcome.Success, task);

    public static UpdateTaskProgressResult TaskNotFound() => new(UpdateTaskProgressOutcome.TaskNotFound);
}