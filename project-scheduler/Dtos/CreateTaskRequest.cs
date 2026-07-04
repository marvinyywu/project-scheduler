using System.ComponentModel.DataAnnotations;

namespace project_scheduler.Dtos;

public sealed class CreateTaskRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Duration { get; set; }
}
