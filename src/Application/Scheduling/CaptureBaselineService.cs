using System.Text.Json;
using Domain.Entities;

namespace Application.Scheduling;

public sealed class CaptureBaselineService(ISchedulingUnitOfWork unitOfWork)
{
    public async Task<CaptureBaselineResult> CaptureAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var project = await unitOfWork.FindProjectAsync(projectId, cancellationToken);
        if (project is null)
        {
            return CaptureBaselineResult.ProjectNotFound();
        }

        var tasks = await unitOfWork.GetTasksForProjectAsync(projectId, cancellationToken);
        var snapshot = tasks.Select(t => new BaselineTaskSnapshot(t.Id, t.EarlyStart, t.EarlyFinish)).ToList();

        var baseline = new Baseline
        {
            ProjectId = projectId,
            CapturedAt = DateTimeOffset.UtcNow,
            SnapshotJson = JsonSerializer.Serialize(snapshot),
        };

        unitOfWork.AddBaseline(baseline);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CaptureBaselineResult.Success(baseline);
    }
}