using Domain.Entities;

namespace Application.Scheduling;

public enum CaptureBaselineOutcome
{
    Success,
    ProjectNotFound
}

public sealed record CaptureBaselineResult(CaptureBaselineOutcome Outcome, Baseline? Baseline = null)
{
    public static CaptureBaselineResult Success(Baseline baseline) => new(CaptureBaselineOutcome.Success, baseline);

    public static CaptureBaselineResult ProjectNotFound() => new(CaptureBaselineOutcome.ProjectNotFound);
}