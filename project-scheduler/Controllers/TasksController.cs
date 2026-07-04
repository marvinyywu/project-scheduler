using Application.Scheduling;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_scheduler.Dtos;

namespace project_scheduler.Controllers;

[ApiController]
[Route("api/projects/{projectId:int}/tasks")]
public sealed class TasksController(SchedulingDbContext db, AddTaskService addTaskService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TaskResponse>> Create(int projectId, CreateTaskRequest request)
    {
        var result = await addTaskService.AddAsync(projectId, request.Name, request.Duration);

        return result.Outcome switch
        {
            AddTaskOutcome.Created => CreatedAtAction(
                nameof(GetById),
                new { projectId, id = result.Task!.Id },
                TaskResponse.FromEntity(result.Task)),
            AddTaskOutcome.ProjectNotFound => NotFound(),
            _ => Problem()
        };
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskResponse>>> GetByProject(int projectId)
    {
        var projectExists = await db.Projects.AnyAsync(p => p.Id == projectId);
        if (!projectExists)
        {
            return NotFound();
        }

        var tasks = await db.Tasks.Where(t => t.ProjectId == projectId).ToListAsync();
        return Ok(tasks.Select(TaskResponse.FromEntity).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TaskResponse>> GetById(int projectId, int id)
    {
        var task = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId);
        return task is null ? NotFound() : Ok(TaskResponse.FromEntity(task));
    }
}
