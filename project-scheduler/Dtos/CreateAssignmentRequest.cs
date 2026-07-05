using System.ComponentModel.DataAnnotations;

namespace project_scheduler.Dtos;

public sealed class CreateAssignmentRequest
{
    [Range(1, int.MaxValue)]
    public int TaskId { get; set; }

    [Range(1, int.MaxValue)]
    public int ResourceId { get; set; }

    [Range(1, int.MaxValue)]
    public int Units { get; set; } = 1;
}
