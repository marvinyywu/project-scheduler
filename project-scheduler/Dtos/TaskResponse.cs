using Domain.Entities;

namespace project_scheduler.Dtos;

public sealed record TaskResponse(
    int Id,
    string Name,
    int Duration,
    int ProjectId,
    int EarlyStart,
    int EarlyFinish,
    int LateStart,
    int LateFinish,
    int TotalFloat,
    int FreeFloat,
    bool IsCritical,
    decimal Budget,
    double PercentComplete,
    decimal ActualCost)
{
    public static TaskResponse FromEntity(ScheduleTask task) => new(
        task.Id,
        task.Name,
        task.Duration,
        task.ProjectId,
        task.EarlyStart,
        task.EarlyFinish,
        task.LateStart,
        task.LateFinish,
        task.TotalFloat,
        task.FreeFloat,
        task.IsCritical,
        task.Budget,
        task.PercentComplete,
        task.ActualCost);
}
