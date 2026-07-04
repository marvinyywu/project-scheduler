using System.ComponentModel.DataAnnotations;

namespace project_scheduler.Dtos;

public sealed class CreateProjectRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateOnly StartDate { get; set; }
}
