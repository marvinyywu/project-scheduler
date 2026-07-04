using System.ComponentModel.DataAnnotations;
using Domain.Entities;

namespace project_scheduler.Dtos;

public sealed class CreateDependencyRequest
{
    [Range(1, int.MaxValue)]
    public int PredecessorId { get; set; }

    [Range(1, int.MaxValue)]
    public int SuccessorId { get; set; }

    public DependencyType Type { get; set; } = DependencyType.FinishToStart;

    [Range(0, int.MaxValue)]
    public int LagDays { get; set; }
}
