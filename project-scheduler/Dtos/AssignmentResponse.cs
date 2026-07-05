using Domain.Entities;

namespace project_scheduler.Dtos;

public sealed record AssignmentResponse(int Id, int TaskId, int ResourceId, int Units)
{
    public static AssignmentResponse FromEntity(Assignment assignment) => new(assignment.Id, assignment.TaskId, assignment.ResourceId, assignment.Units);
}
