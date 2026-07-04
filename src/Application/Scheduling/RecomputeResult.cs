namespace Application.Scheduling;

public enum RecomputeOutcome
{
    Success,
    ProjectNotFound,
    CycleDetected
}

public sealed record RecomputeResult(RecomputeOutcome Outcome, int ProjectDuration = 0)
{
    public static RecomputeResult Success(int projectDuration) => new(RecomputeOutcome.Success, projectDuration);

    public static RecomputeResult ProjectNotFound() => new(RecomputeOutcome.ProjectNotFound);

    public static RecomputeResult CycleDetected() => new(RecomputeOutcome.CycleDetected);
}
