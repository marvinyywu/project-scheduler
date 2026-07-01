namespace Domain.Entities;

public sealed class Dependency
{
    public int Id { get; init; }
    public int PredecessorId { get; init; }
    public int SuccessorId { get; init; }
    public DependencyType Type { get; init; } = DependencyType.FinishToStart;
    public int LagDays { get; init; }
}
