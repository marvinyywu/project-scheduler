using Domain.Entities;

namespace Application.Scheduling;

public enum AddDependencyOutcome
{
    Created,
    TaskNotFound,
    CycleDetected
}

public sealed record AddDependencyResult(AddDependencyOutcome Outcome, Dependency? Dependency = null)
{
    public static AddDependencyResult Success(Dependency dependency) => new(AddDependencyOutcome.Created, dependency);

    public static AddDependencyResult TaskNotFound() => new(AddDependencyOutcome.TaskNotFound);

    public static AddDependencyResult CycleDetected() => new(AddDependencyOutcome.CycleDetected);
}
