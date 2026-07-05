using Domain.Cost;

namespace Application.Scheduling;

public enum ComputeEvmOutcome
{
    Success,
    ProjectNotFound
}

public sealed record ComputeEvmResult(ComputeEvmOutcome Outcome, EvmReport Report = default)
{
    public static ComputeEvmResult Success(EvmReport report) => new(ComputeEvmOutcome.Success, report);

    public static ComputeEvmResult ProjectNotFound() => new(ComputeEvmOutcome.ProjectNotFound);
}