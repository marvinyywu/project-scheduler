namespace Domain.Entities;

public sealed class ScheduleTask
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Duration { get; init; }

    public int EarlyStart { get; set; }
    public int EarlyFinish { get; set; }
    public int LateStart { get; set; }
    public int LateFinish { get; set; }
    public int TotalFloat { get; set; }
    public int FreeFloat { get; set; }
    public bool IsCritical { get; set; }
    public int ProjectId { get; set; }

    public decimal Budget { get; init; }
    public double PercentComplete { get; set; }
    public decimal ActualCost { get; set; }
}
