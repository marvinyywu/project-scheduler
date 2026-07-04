using System.Text.Json.Serialization;
using Application.Scheduling;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

const string AngularDevCors = "AngularDev";

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

builder.Services.AddCors(options =>
    options.AddPolicy(AngularDevCors, policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors(AngularDevCors);

app.UseAuthorization();

app.MapControllers();

app.Run();
