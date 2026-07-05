# Project Scheduling & Resource Management App

## Problem

Teams running dependent tasks with shared people can't see what drives the
end date, who's overloaded, or whether they're over budget. This app builds
a task network, computes the critical path and float, levels resources, and
reports Earned Value cost metrics on a live Gantt.

Full project scope and rationale: [`project-scheduler/Project_Scheduling_App_Project_Pack.md`](project-scheduler/Project_Scheduling_App_Project_Pack.md).

## Architecture

```text
Angular SPA (web/) -- Material, signal-based ScheduleStore, SVG + D3-scale Gantt,
    |                  baseline drift bars, EVM dashboard
    | HTTPS / JSON
    v
ASP.NET Core Web API (project-scheduler/) -- DTOs, controllers, OpenAPI + Scalar UI
    |
    v
Application layer (src/Application) -- AddTaskService, AddDependencyService,
    |                                   RecomputeScheduleService, LevelScheduleService,
    |                                   UpdateTaskProgressService, ComputeEvmService,
    |                                   CaptureBaselineService
    +--> Domain / Scheduling Engine  (CPM forward+backward pass, float,
    |                                  priority-rule resource leveling, EVM) -- NO framework deps
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

- Engine: plain C# class library (`src/Domain`) — CPM, a priority-rule
  resource leveler, and an `EvmCalculator` (`src/Domain/Cost`)
- Application: use-case services (`src/Application/Scheduling`) orchestrating the engine and persistence
- Tests: xUnit (`tests/Domain.Tests`, `tests/Application.Tests`)
- API: ASP.NET Core Web API (`project-scheduler/`) — DTOs and controllers wired to the Application layer, with an OpenAPI document and a Scalar UI for exploring it
- Frontend: Angular (`web/`) — standalone components, Angular Material, a
  signal-based `ScheduleStore`, an SVG Gantt using a `d3-scale` linear scale
  over project-day with baseline drift bars, a resource histogram flagging
  over-allocation, and an EVM dashboard with status colours
- Data: EF Core + SQL Server LocalDB (`src/Infrastructure`), migrations under `src/Infrastructure/Migrations`

## Status: Week 5 (budget, baseline, and EVM)

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
- `Resource`/`Assignment` entities and EF Core mapping — `Resources` and
  `Assignments` tables, `Assignments` cascading on `TaskId` and restricting
  on `ResourceId` (`src/Domain/Entities`, `src/Infrastructure/Persistence`)
- `ResourceLeveler`: a priority-list-scheduling heuristic (lowest total float
  first, walking the dependency DAG in topological order, delaying a task
  day-by-day only when an assigned resource is full) that returns leveled
  start/finish dates without mutating the authoritative CPM schedule
  (`src/Domain/Scheduling`)
- `ResourceLevelerTests` extending the section 1.4 worked example with a
  resource shared by two parallel tasks — proving the leveler both resolves
  a real over-allocation (pushing the project finish from 12 to 14 days) and
  leaves the schedule untouched when capacity is sufficient
- Application-layer `AddResourceService`, `AssignResourceService`, and
  `LevelScheduleService` (`src/Application/Scheduling`), plus
  `ResourcesController` and `AssignmentsController` exposing them over HTTP
- A resource panel (add-resource and assign-resource forms, a "Level
  resources" action with a before/after project-finish summary) and a
  resource histogram (SVG stacked bars per resource per day, red when usage
  exceeds capacity) — the histogram is a client-side aggregation over
  already-fetched data, not a server call, the same way `criticalTaskIds`
  and `projectDuration` are computed (`web/src/app/features`,
  `web/src/app/core/state`)
- Browser smoke test: assigned a capacity-limited resource to two tasks that
  CPM schedules in parallel, confirmed the histogram flags the
  over-allocation, ran leveling, and confirmed both the before/after summary
  (12 → 14 days) and the now-clear histogram match `ResourceLevelerTests`
- `ScheduleTask` gains `Budget` (set once at creation, planning-time),
  `PercentComplete`, and `ActualCost` (updated repeatedly as work
  progresses, status-time) — `Budget`/`ActualCost` mapped as
  `decimal(18,2)` (`src/Domain/Entities`, `src/Infrastructure/Persistence/Configurations`)
- `Baseline` entity storing a point-in-time JSON snapshot of every task's
  `EarlyStart`/`EarlyFinish` (`Baselines` table, cascading on `ProjectId`) —
  a snapshot rather than parallel `BaselineStart`/`BaselineFinish` columns,
  so capturing a second or third baseline needs no schema change
  (`src/Domain/Entities`, `src/Infrastructure/Persistence`)
- `EvmCalculator`: a framework-free static method computing PV (linear
  interpolation of budget across a task's `EarlyStart`/`EarlyFinish` window
  against a status-date project-day), EV (`Budget × PercentComplete`), and
  the AC/SV/CV/SPI/CPI/EAC/ETC/VAC roll-up (`src/Domain/Cost`)
- `EvmCalculatorTests` reproducing a hand-worked EVM table extending the
  section 1.4 worked example (BAC 1400, PV 700, EV 740, AC 750 at status
  day 5, EAC ≈1418.92), plus zero-PV and zero-AC edge cases proving SPI/CPI
  come back `0` instead of dividing by zero
- Application-layer `UpdateTaskProgressService`, `ComputeEvmService`, and
  `CaptureBaselineService` (`src/Application/Scheduling`), plus a
  `ComputeEvm_MatchesHandWorkedExample_AfterPersistenceRoundTrip` test
  proving the same hand-worked numbers survive a full persistence
  round-trip through a fresh `DbContext`, the same pattern Week 2
  established for CPM
- New endpoints: `PATCH .../tasks/{id}/progress`, `GET .../evm?asOfDay=`,
  `POST .../baseline`, `GET .../baseline` (`ProjectsController`,
  `TasksController`)
- An `EvmDashboard` feature component — a task-progress form (percent
  complete + actual cost), an "as of day" input with Refresh/Capture
  baseline actions, and a stat grid for BAC/PV/EV/AC/SV/CV/SPI/CPI/EAC/ETC/VAC
  that turns SPI or CPI red below 1.0 — plus a Budget field and column added
  to the task list, since `Budget` is otherwise unreachable from the UI
  (`web/src/app/features/evm-dashboard`, `web/src/app/features/task-list`)
- Baseline drift bars on the Gantt: a `baselineBars` computed signal renders
  a thin grey bar from the captured snapshot beneath each task's current bar
  (`web/src/app/features/gantt-chart`)
- Browser smoke test: captured a baseline on the section 1.4 network, added
  a new task with a budget and wired it in front of task A (shifting the
  whole network +2 days on recompute), and confirmed the grey baseline bars
  stayed at the original dates while the current bars moved — then drove
  the progress form and confirmed the EVM stat grid matched hand
  computation exactly (EV $75/AC $60/SPI 1.50/CPI 1.25 and, at 10%/$200,
  EV $10/AC $200/SPI 0.20/CPI 0.05 rendering both red)

Not yet built: working-day calendars (the Gantt's x-axis is still linear
project-day offsets, not calendar dates), Gantt dependency connector arrows,
a project picker/routing, and deployment. See the six-week plan in the
project pack for what's next.

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
