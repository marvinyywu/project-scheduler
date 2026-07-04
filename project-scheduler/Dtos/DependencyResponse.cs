using Domain.Entities;

namespace project_scheduler.Dtos;

public sealed record DependencyResponse(int Id, int PredecessorId, int SuccessorId, DependencyType Type, int LagDays)
{
    public static DependencyResponse FromEntity(Dependency dependency) => new(
        dependency.Id,
        dependency.PredecessorId,
        dependency.SuccessorId,
        dependency.Type,
        dependency.LagDays);
}
