using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class SchedulingDbContext(DbContextOptions<SchedulingDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ScheduleTask> Tasks => Set<ScheduleTask>();
    public DbSet<Dependency> Dependencies => Set<Dependency>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Assignment> Assignments => Set<Assignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
}
