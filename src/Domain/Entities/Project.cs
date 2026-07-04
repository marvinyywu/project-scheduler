namespace Domain.Entities;

public sealed class Project
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateOnly StartDate { get; init; }
}
