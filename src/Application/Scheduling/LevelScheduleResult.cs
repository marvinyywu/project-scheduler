using Domain.Scheduling;

namespace Application.Scheduling;

public enum LevelScheduleOutcome
{
    Success,
    ProjectNotFound
}

public sealed record LevelScheduleResult(
    LevelScheduleOutcome Outcome,
    int OriginalProjectDuration = 0,
    int LeveledProjectDuration = 0,
    IReadOnlyList<LeveledTask>? LeveledTasks = null)
{
    public static LevelScheduleResult Success(int originalDuration, int leveledDuration, IReadOnlyList<LeveledTask> leveledTasks)
        => new(LevelScheduleOutcome.Success, originalDuration, leveledDuration, leveledTasks);

    public static LevelScheduleResult ProjectNotFound() => new(LevelScheduleOutcome.ProjectNotFound);
}
