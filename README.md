# Project Scheduling & Resource Management App

## Problem

Teams running dependent tasks with shared people can't see what drives the
end date, who's overloaded, or whether they're over budget. This app builds
a task network, computes the critical path and float, levels resources, and
reports Earned Value cost metrics on a live Gantt.

Full project scope and rationale: [`project-scheduler/Project_Scheduling_App_Project_Pack.md`](project-scheduler/Project_Scheduling_App_Project_Pack.md).

## Architecture

```text
Angular SPA (web/) -- Material, signal-based ScheduleStore, SVG + D3-scale Gantt
    | HTTPS / JSON
    v
ASP.NET Core Web API (project-scheduler/) -- DTOs, controllers, OpenAPI + Scalar UI
    |
    v
Application layer (src/Application) -- AddTaskService, AddDependencyService,
    |                                   RecomputeScheduleService
    +--> Domain / Scheduling Engine  (CPM forward+backward pass, float) -- NO framework deps
    |
    +--> Infrastructure / EF Core (src/Infrastructure) -- SQL Server LocalDB
```

The scheduling engine (`src/Domain`) is a plain C# class library with zero
framework dependencies, so it can be unit-tested without a database, API, or
UI. The Application layer only depends on Domain too — it talks to
persistence through an `ISchedulingUnitOfWork` interface that `Infrastructure`
implements, so `CpmEngine` stays the only thing that ever touches the engine
outside of `Domain.Tests`.

## Stack

- Engine: plain C# class library (`src/Domain`) — CPM today; leveling and EVM planned
- Application: use-case services (`src/Application/Scheduling`) orchestrating the engine and persistence
- Tests: xUnit (`tests/Domain.Tests`, `tests/Application.Tests`)
- API: ASP.NET Core Web API (`project-scheduler/`) — DTOs and controllers wired to the Application layer, with an OpenAPI document and a Scalar UI for exploring it
- Frontend: Angular (`web/`) — standalone components, Angular Material, a
  signal-based `ScheduleStore`, and an SVG Gantt using a `d3-scale` linear
  scale over project-day
- Data: EF Core + SQL Server LocalDB (`src/Infrastructure`), migrations under `src/Infrastructure/Migrations`

## Status: Week 3 (the Angular app and the Gantt)

Built so far:

- `Task`/`Dependency` entities (`src/Domain/Entities`)
- Cycle detection, topological sort, and a CPM engine with forward pass,
  backward pass, total float, free float, and critical-path flagging,
  supporting all four relationship types (FS/SS/FF/SF) with lag
  (`src/Domain/Scheduling`)
- `Domain.Tests` reproducing the hand-worked example from the project pack
  (section 1.4), a cycle-rejection test, and a relationship-type/lag test —
  all passing
- `Project` entity and EF Core mapping for `Project`/`ScheduleTask`/`Dependency`
  against SQL Server LocalDB, including the self-referencing `Dependencies`
  table with `Restrict` on both FKs (`src/Infrastructure/Persistence`)
- Application-layer use-case services — `AddTaskService`,
  `AddDependencyService` (rejects cyclic dependencies before they're ever
  persisted), and `RecomputeScheduleService` — plus `Application.Tests`
  proving the worked example survives a full persistence round-trip
  (`src/Application/Scheduling`)
- ASP.NET Core controllers (`ProjectsController`, `TasksController`,
  `DependenciesController`) exposing DTOs over the Application layer, with
  201/400/404/409 mapped from validation and use-case results
- OpenAPI document at `/openapi/v1.json` and a browsable Scalar UI at
  `/scalar/v1`
- End-to-end smoke test: created a project, added tasks A/B/C/D, wired up
  the four FS dependencies, recomputed, and confirmed the persisted
  ES/EF/LS/LF/float/critical values match `Domain.Tests` exactly
- Angular app scaffolded (`web/`) with Angular Material and standalone
  components — no NgModules, no router (one page is the whole Week 3
  deliverable)
- CORS enabled in `Program.cs`, scoped to the Angular dev server origin
  (`http://localhost:4200`)
- TypeScript DTOs mirroring the API's JSON shapes (`web/src/app/core/models`)
  and a thin `SchedulingApiService` wrapping `HttpClient` in
  promise-returning methods (`web/src/app/core/api`)
- A signal-based `ScheduleStore` (`web/src/app/core/state`) holding
  project/task/error state and computed `criticalTaskIds`/`projectDuration`
  — every mutation re-fetches the task list from the API rather than
  recomputing CPM client-side
- Task list with an "add task" form, a dependency editor with a snackbar for
  the 409-cycle case, and an SVG Gantt (linear `d3-scale` over project-day,
  critical path in red), all composed into one `SchedulePage`
  (`web/src/app/features`)
- Browser smoke test: rebuilt the section 1.4 worked example through the UI
  and confirmed the Gantt bars, critical-path highlighting, and cycle
  rejection all match the curl-driven Week 2 result

Not yet built: working-day calendars (the Gantt's x-axis is still linear
project-day offsets, not calendar dates), Gantt dependency connector arrows,
a project picker/routing, resource leveling, EVM, baselines, and deployment.
See the six-week plan in the project pack for what's next.

## How to Run

```bash
# Run all tests
dotnet test tests/Domain.Tests/Domain.Tests.csproj
dotnet test tests/Application.Tests/Application.Tests.csproj

# Apply migrations to a local SQL Server LocalDB instance (fresh clone / first run)
dotnet tool install --global dotnet-ef   # only needed once per machine
dotnet ef database update --project src/Infrastructure --startup-project project-scheduler

# Run the API
dotnet run --project project-scheduler
```

With the API running, open `http://localhost:5008/scalar/v1` (or the HTTPS
URL from `project-scheduler/Properties/launchSettings.json`) for a browsable,
self-documenting view of every endpoint.

```bash
# Run the Angular app (first time: cd web && npm install)
cd web
npm start
```

The API and the Angular dev server (`http://localhost:4200`) both need to be
running at the same time in development — the SPA calls the API over HTTP,
and `Program.cs` only allows CORS requests from `http://localhost:4200`
specifically, so the dev server has to be up for the browser to be able to
reach the API at all.
