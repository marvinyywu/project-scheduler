namespace Domain.Entities;

public sealed class Resource
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int MaxUnitsPerDay { get; init; } = 1;
}