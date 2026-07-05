using Application.Scheduling;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_scheduler.Dtos;

namespace project_scheduler.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(
    SchedulingDbContext db,
    RecomputeScheduleService recomputeScheduleService,
    LevelScheduleService levelScheduleService,
    ComputeEvmService computeEvmService,
    CaptureBaselineService captureBaselineService) : ControllerBase
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

    [HttpGet("{id:int}/assignments")]
    public async Task<ActionResult<IReadOnlyList<AssignmentResponse>>> GetAssignments(int id)
    {
        var taskIds = await db.Tasks.Where(t => t.ProjectId == id).Select(t => t.Id).ToListAsync();
        var assignments = await db.Assignments.Where(a => taskIds.Contains(a.TaskId)).ToListAsync();
        return Ok(assignments.Select(AssignmentResponse.FromEntity).ToList());
    }

    [HttpPost("{id:int}/level")]
    public async Task<ActionResult<LevelingResponse>> Level(int id)
    {
        var result = await levelScheduleService.LevelAsync(id);
        return result.Outcome switch
        {
            LevelScheduleOutcome.Success => Ok(new LevelingResponse(
                result.OriginalProjectDuration,
                result.LeveledProjectDuration,
                result.LeveledTasks!.Select(LeveledTaskResponse.FromDomain).ToList())),
            LevelScheduleOutcome.ProjectNotFound => NotFound(),
            _ => Problem()
        };
    }

    [HttpGet("{id:int}/evm")]
    public async Task<ActionResult<EvmReportResponse>> GetEvm(int id, [FromQuery] int asOfDay)
    {
        var result = await computeEvmService.ComputeAsync(id, asOfDay);

        return result.Outcome switch
        {
            ComputeEvmOutcome.Success => Ok(EvmReportResponse.FromDomain(result.Report)),
            ComputeEvmOutcome.ProjectNotFound => NotFound(),
            _ => Problem()
        };
    }

    [HttpPost("{id:int}/baseline")]
    public async Task<ActionResult<BaselineResponse>> CaptureBaseline(int id)
    {
        var result = await captureBaselineService.CaptureAsync(id);

        return result.Outcome switch
        {
            CaptureBaselineOutcome.Success => Ok(BaselineResponse.FromEntity(result.Baseline!)),
            CaptureBaselineOutcome.ProjectNotFound => NotFound(),
            _ => Problem()
        };
    }

    [HttpGet("{id:int}/baseline")]
    public async Task<ActionResult<BaselineResponse>> GetLatestBaseline(int id)
    {
        var baseline = await db.Baselines
            .Where(b => b.ProjectId == id)
            .OrderByDescending(b => b.CapturedAt)
            .FirstOrDefaultAsync();

        return baseline is null ? NotFound() : Ok(BaselineResponse.FromEntity(baseline));
    }
}
