namespace project_scheduler.Dtos;

public sealed record LevelingResponse(int OriginalProjectDuration, int LeveledProjectDuration, IReadOnlyList<LeveledTaskResponse> Tasks);
