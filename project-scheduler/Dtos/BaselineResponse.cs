using System.Text.Json;
using Application.Scheduling;
using Domain.Entities;

namespace project_scheduler.Dtos;

public sealed record BaselineTaskResponse(int TaskId, int EarlyStart, int EarlyFinish);

public sealed record BaselineResponse(DateTimeOffset CapturedAt, IReadOnlyList<BaselineTaskResponse> Tasks)
{
    public static BaselineResponse FromEntity(Baseline baseline)
    {
        var snapshot = JsonSerializer.Deserialize<List<BaselineTaskSnapshot>>(baseline.SnapshotJson) ?? [];
        var tasks = snapshot.Select(s => new BaselineTaskResponse(s.TaskId, s.EarlyStart, s.EarlyFinish)).ToList();
        return new BaselineResponse(baseline.CapturedAt, tasks);
    }
}