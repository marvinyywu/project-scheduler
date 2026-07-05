namespace Domain.Entities;

public sealed class Baseline
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public DateTimeOffset CapturedAt { get; init; }
    public string SnapshotJson { get; init; } = string.Empty;
}