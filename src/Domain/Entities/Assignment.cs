namespace Domain.Entities;

public sealed class Assignment
{
    public int Id { get; init; }
    public int TaskId { get; init; }
    public int ResourceId { get; init; }
    public int Units { get; init; } = 1;
}