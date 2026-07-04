using Domain.Entities;

namespace Application.Scheduling;

public enum AddTaskOutcome
{
    Created,
    ProjectNotFound
}

public sealed record AddTaskResult(AddTaskOutcome Outcome, ScheduleTask? Task = null)
{
    public static AddTaskResult Success(ScheduleTask task) => new(AddTaskOutcome.Created, task);

    public static AddTaskResult ProjectNotFound() => new(AddTaskOutcome.ProjectNotFound);
}
