using Domain.Scheduling;

namespace project_scheduler.Dtos;

public sealed record LeveledTaskResponse(int TaskId, int LeveledStart, int LeveledFinish, int DelayDays)
{
    public static LeveledTaskResponse FromDomain(LeveledTask leveled) => new(leveled.TaskId, leveled.LeveledStart, leveled.LeveledFinish, leveled.Delay);
}
