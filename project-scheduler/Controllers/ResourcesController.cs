using Application.Scheduling;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using project_scheduler.Dtos;

namespace project_scheduler.Controllers;

[ApiController]
[Route("api/resources")]
public sealed class ResourcesController(SchedulingDbContext db, AddResourceService addResourceService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ResourceResponse>> Create(CreateResourceRequest request)
    {
        var resource = await addResourceService.AddAsync(request.Name, request.MaxUnitsPerDay);
        return CreatedAtAction(nameof(GetById), new { id = resource.Id }, ResourceResponse.FromEntity(resource));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ResourceResponse>>> GetAll()
    {
        var resources = await db.Resources.ToListAsync();
        return Ok(resources.Select(ResourceResponse.FromEntity).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ResourceResponse>> GetById(int id)
    {
        var resource = await db.Resources.FindAsync(id);
        return resource is null ? NotFound() : Ok(ResourceResponse.FromEntity(resource));
    }
}
