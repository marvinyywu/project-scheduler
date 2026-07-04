using Application.Scheduling;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using project_scheduler.Dtos;

namespace project_scheduler.Controllers;

[ApiController]
[Route("api/dependencies")]
public sealed class DependenciesController(SchedulingDbContext db, AddDependencyService addDependencyService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<DependencyResponse>> Create(CreateDependencyRequest request)
    {
        var result = await addDependencyService.AddAsync(request.PredecessorId, request.SuccessorId, request.Type, request.LagDays);

        return result.Outcome switch
        {
            AddDependencyOutcome.Created => CreatedAtAction(
                nameof(GetById),
                new { id = result.Dependency!.Id },
                DependencyResponse.FromEntity(result.Dependency)),
            AddDependencyOutcome.TaskNotFound => NotFound(),
            AddDependencyOutcome.CycleDetected => Conflict(),
            _ => Problem()
        };
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DependencyResponse>> GetById(int id)
    {
        var dependency = await db.Dependencies.FindAsync(id);
        return dependency is null ? NotFound() : Ok(DependencyResponse.FromEntity(dependency));
    }
}
