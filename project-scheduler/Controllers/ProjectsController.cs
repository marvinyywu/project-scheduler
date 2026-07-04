using Application.Scheduling;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using project_scheduler.Dtos;

namespace project_scheduler.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(SchedulingDbContext db, RecomputeScheduleService recomputeScheduleService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ProjectResponse>> Create(CreateProjectRequest request)
    {
        var project = new Project { Name = request.Name, StartDate = request.StartDate };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = project.Id }, ProjectResponse.FromEntity(project));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectResponse>> GetById(int id)
    {
        var project = await db.Projects.FindAsync(id);
        return project is null ? NotFound() : Ok(ProjectResponse.FromEntity(project));
    }

    [HttpPost("{id:int}/recompute")]
    public async Task<ActionResult<RecomputeResponse>> Recompute(int id)
    {
        var result = await recomputeScheduleService.RecomputeAsync(id);

        return result.Outcome switch
        {
            RecomputeOutcome.Success => Ok(new RecomputeResponse(result.ProjectDuration)),
            RecomputeOutcome.ProjectNotFound => NotFound(),
            RecomputeOutcome.CycleDetected => Conflict(),
            _ => Problem()
        };
    }
}
