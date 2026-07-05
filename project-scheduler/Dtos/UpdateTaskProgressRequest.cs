using System.ComponentModel.DataAnnotations;

namespace project_scheduler.Dtos;

public sealed class UpdateTaskProgressRequest
{
    [Range(0, 100)]
    public double PercentComplete { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ActualCost { get; set; }
}