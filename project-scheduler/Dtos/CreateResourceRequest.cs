using System.ComponentModel.DataAnnotations;

namespace project_scheduler.Dtos;

public sealed class CreateResourceRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int MaxUnitsPerDay { get; set; } = 1;
}
