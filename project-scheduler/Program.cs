using System.Text.Json.Serialization;
using Application.Scheduling;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<SchedulingDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Scheduling")));

builder.Services.AddScoped<ISchedulingUnitOfWork, SchedulingUnitOfWork>();
builder.Services.AddScoped<AddTaskService>();
builder.Services.AddScoped<AddDependencyService>();
builder.Services.AddScoped<RecomputeScheduleService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
