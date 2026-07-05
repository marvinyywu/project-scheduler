using Application.Scheduling;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using project_scheduler.Dtos;

namespace project_scheduler.Controllers;

[ApiController]
[Route("api/assignments")]
public sealed class AssignmentsController(SchedulingDbContext db, AssignResourceService assignResourceService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AssignmentResponse>> Create(CreateAssignmentRequest request)
    {
        var result = await assignResourceService.AssignAsync(request.TaskId, request.ResourceId, request.Units);
        return result.Outcome switch
        {
            AssignResourceOutcome.Created => CreatedAtAction(nameof(GetById), new { id = result.Assignment!.Id }, AssignmentResponse.FromEntity(result.Assignment)),
            AssignResourceOutcome.TaskNotFound => NotFound("Task not found."),
            AssignResourceOutcome.ResourceNotFound => NotFound("Resource not found."),
            _ => Problem()
        };
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AssignmentResponse>> GetById(int id)
    {
        var assignment = await db.Assignments.FindAsync(id);
        return assignment is null ? NotFound() : Ok(AssignmentResponse.FromEntity(assignment));
    }
}
