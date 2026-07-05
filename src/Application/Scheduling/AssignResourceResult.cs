using Domain.Entities;

namespace Application.Scheduling;

public enum AssignResourceOutcome
{
    Created,
    TaskNotFound,
    ResourceNotFound
}

public sealed record AssignResourceResult(AssignResourceOutcome Outcome, Assignment? Assignment = null)
{
    public static AssignResourceResult Success(Assignment assignment) => new(AssignResourceOutcome.Created, assignment);

    public static AssignResourceResult TaskNotFound() => new(AssignResourceOutcome.TaskNotFound);

    public static AssignResourceResult ResourceNotFound() => new(AssignResourceOutcome.ResourceNotFound);
}
