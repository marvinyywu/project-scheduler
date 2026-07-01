# Full-Stack Engineering Project Pack — Project: Project Scheduling & Resource Management App

Prepared for a developer building a portfolio-grade, production-shaped scheduling application with C#, ASP.NET Core, and Angular. This pack is written in the same style as the RAG Knowledge Base pack and is built to close a specific set of skill gaps: domain algorithms, clean full-stack architecture, and honest engineering practices.

## 0. Executive Summary

This project proves the skills that show up in almost every enterprise .NET full-stack job description: designing a non-trivial domain model, implementing real algorithms (not just CRUD), building a typed REST API, and wiring it to a reactive Angular frontend. Where a typical "todo app" proves you can move data, this project proves you can model a hard problem and compute over it.

Gaps this project closes:

- Backend engineering (high priority): C#, ASP.NET Core Web API, Entity Framework Core, layered/clean architecture, dependency injection.
- Domain algorithms (high priority): Critical Path Method (CPM), schedule network analysis, resource leveling, Earned Value Management (EVM).
- Frontend engineering (high priority): Angular, TypeScript, reactive state, data-dense visualization (Gantt and resource histograms).
- System design (medium-high priority): separation of the scheduling engine from the API, persistence, and UI, with a documented architecture diagram.
- DevOps and cloud (medium priority): containerization, CI, and deployment to a cloud platform with a managed database.

Recommended main project:

> Project Scheduling & Resource Management App: a web app where a user builds a project schedule (a Work Breakdown Structure of tasks with dependencies), and the system computes the critical path and float, assigns and levels resources, tracks progress against a baseline, manages budgets with Earned Value metrics, and visualizes workload. Built on ASP.NET Core + Angular, deployed to the cloud.

This project is strong because scheduling is a genuinely hard computational problem (it forces real algorithm and data-structure work), the architecture forces real system-design tradeoffs, and it can be demonstrated live: add tasks, draw a dependency, watch the critical path recompute.

What this project is **not**:

- It is not a clone of Oracle Primavera P6 or Microsoft Project. Those are decades of work by large teams.
- It is not a full resource-optimization solver. Resource leveling is NP-hard; you implement documented heuristics, not an optimal solver.
- It is not a multi-tenant SaaS with billing, SSO, and audit compliance.
- It is a focused, production-shaped engineering project that models the core of a scheduling tool correctly and honestly.

A one-line scope guardrail: build the **scheduling engine and the data-dense UI on top of it** extremely well, and treat everything else (auth, multi-project portfolios, integrations) as optional polish.

## 1. Skills Required by the Stack and Domain

### 1.1 C# and modern .NET fundamentals

You should be comfortable writing C# beyond tutorials.

What to know:

- types, generics, interfaces, records, nullable reference types,
- LINQ (this is central — schedule analysis is full of grouping, ordering, and aggregation),
- `async`/`await` for I/O-bound API and database work,
- dependency injection (built into ASP.NET Core),
- exceptions, `Result`-style error handling, and validation.

Target level:

You do not need to be a senior backend engineer. You do need to structure a multi-project solution, explain every layer, and run the whole thing locally and in the cloud.

Use .NET 10 (the current LTS, released November 2025, with C# 14 and Visual Studio 2026). LTS means three years of support, which is the right default for anything you want to show or maintain.

Practice target:

- A `Domain` class library with the scheduling model and engine, no framework dependencies.
- An `Application` layer with use-case services (create task, add dependency, recompute schedule).
- An `Api` project (ASP.NET Core) exposing the use cases over HTTP.
- An `Infrastructure` project with EF Core persistence.

### 1.2 ASP.NET Core Web API

This is how the Angular frontend talks to your domain.

What to know:

- controllers vs minimal APIs (either is fine; pick one and be consistent),
- routing, model binding, and `[ApiController]` conventions,
- DTOs vs domain entities, and why you never expose entities directly,
- validation (`FluentValidation` or data annotations),
- returning correct status codes (`200`, `201`, `400`, `404`, `409`),
- OpenAPI/Swagger for a self-documenting API,
- CORS configuration so Angular can call the API in development.

Interview wording:

> The API is a thin adapter. Controllers validate input, map DTOs to commands, call an application service, and map the result back to a DTO. No scheduling logic lives in a controller.

Recommended learning:

- ASP.NET Core Web API tutorial: https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api
- Minimal APIs overview: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview

### 1.3 Entity Framework Core and data modeling

The schedule is a graph, and you have to store it relationally. That tension is the interesting part.

What to know:

- `DbContext`, entities, and configuration via Fluent API,
- one-to-many and many-to-many relationships,
- self-referencing relationships (a task dependency links a task to another task),
- migrations (`dotnet ef migrations add`, `dotnet ef database update`),
- eager vs lazy vs explicit loading, and the N+1 query trap,
- transactions for multi-row updates (recomputing and saving an entire schedule).

The core tables:

```text
Projects        (Id, Name, StartDate, CalendarId, BudgetAtCompletion)
Tasks           (Id, ProjectId, WbsCode, Name, Duration, PercentComplete,
                 EarlyStart, EarlyFinish, LateStart, LateFinish, TotalFloat,
                 IsCritical, BaselineStart, BaselineFinish, IsMilestone)
Dependencies    (Id, PredecessorId, SuccessorId, Type, LagDays)   -- the edges
Resources       (Id, Name, Type, CostPerHour, MaxUnitsPerDay)
Assignments     (Id, TaskId, ResourceId, Units, PlannedHours, ActualHours)
Calendars       (Id, Name)  + WorkingDays + Holidays
Baselines       (Id, ProjectId, CapturedAt, SnapshotJson)
```

Interview wording:

> A schedule is a directed acyclic graph, but it has to persist in a relational store. I model tasks as nodes and dependencies as a self-referencing edge table with a relationship type and lag, then load the graph into memory to run the engine.

### 1.4 The scheduling engine — Critical Path Method (the technical heart)

This is the area that separates this project from a CRUD app, and the area most beginner versions get subtly wrong.

What to know:

- a schedule network is a **directed acyclic graph** (DAG): tasks are nodes, dependencies are edges,
- **topological sort** orders tasks so every task comes after its predecessors,
- the **forward pass** computes Early Start (ES) and Early Finish (EF),
- the **backward pass** computes Late Start (LS) and Late Finish (LF),
- **total float** = LS − ES = LF − EF (how long a task can slip without delaying the project),
- the **critical path** is the chain of tasks with zero total float,
- **free float** = (earliest successor ES) − EF (slack that doesn't affect any successor),
- **cycle detection**: if the graph has a cycle, there is no valid schedule and you must reject it.

The four dependency relationship types (you should support at least Finish-to-Start, then add the rest):

- **FS** (Finish-to-Start): successor starts after predecessor finishes. The default.
- **SS** (Start-to-Start): successor starts after predecessor starts.
- **FF** (Finish-to-Finish): successor finishes after predecessor finishes.
- **SF** (Start-to-Finish): rare; successor finishes after predecessor starts.

Each relationship can carry a **lag** (positive delay) or **lead** (negative lag).

A worked FS example you can use as a correctness test:

```text
Task  Dur  Predecessors
A     3    -
B     4    A (FS)
C     2    A (FS)
D     5    B, C (FS)

Forward pass (project start = day 0):
A: ES 0, EF 3
B: ES 3, EF 7
C: ES 3, EF 5
D: ES max(7,5)=7, EF 12      -> project finish = day 12

Backward pass (from finish = 12):
D: LF 12, LS 7
B: LF 7,  LS 3
C: LF 7,  LS 5
A: LF 3,  LS 0

Total float:
A 0, B 0, C 2, D 0   ->  Critical path: A -> B -> D
```

If your engine reproduces these numbers, the core math is right. If it doesn't, nothing downstream can be trusted.

Interview wording:

> The engine topologically sorts the task graph, runs a forward pass for early dates and a backward pass for late dates, derives total float, and flags zero-float tasks as critical. It rejects cyclic graphs before computing anything.

Recommended learning:

- Critical Path Method overview: https://en.wikipedia.org/wiki/Critical_path_method
- Topological sorting: https://en.wikipedia.org/wiki/Topological_sorting
- PMI scheduling concepts (PMBOK terms): search "PMI critical path method" for the canonical definitions.

### 1.5 Resource management and leveling

Once tasks have dates, you assign people and equipment — and discover they are over-allocated.

What to know:

- a resource has capacity (e.g., max units per day),
- an assignment links a resource to a task with an allocation (units / hours),
- **over-allocation** happens when a resource's total assigned units on a day exceed capacity,
- **resource leveling** delays tasks to remove over-allocation, usually pushing the project finish out,
- leveling is **NP-hard** in general; real tools (P6, MS Project) use heuristics, and so should you.

A standard, defensible heuristic to implement:

- process tasks in priority order (e.g., least total float first, then earliest ES, then WBS order),
- schedule each task as early as possible **subject to** every assigned resource having free capacity,
- if capacity is unavailable, delay the task to the next day capacity frees up,
- record the resulting delay and recompute the schedule.

Interview wording:

> Leveling is NP-hard, so I implemented a priority-rule heuristic: lowest-float-first, scheduling each task as early as resource capacity allows. It is not optimal, and I can show cases where it produces a longer schedule than the theoretical minimum, but it is the same class of approach commercial tools use.

Things not to claim:

- Do not claim your leveler is optimal.
- Do not claim it matches P6 exactly; different tools use different tie-breaking rules.

### 1.6 Budgeting and Earned Value Management (EVM)

This is the cost half of the app and a strong differentiator, because most portfolio projects ignore money entirely.

What to know (the core EVM quantities):

- **BAC** (Budget at Completion): total planned budget.
- **PV** (Planned Value): budgeted cost of work scheduled by a date.
- **EV** (Earned Value): budgeted cost of work actually completed = % complete × task budget.
- **AC** (Actual Cost): what was actually spent.
- **SV** (Schedule Variance) = EV − PV. Negative means behind schedule.
- **CV** (Cost Variance) = EV − AC. Negative means over budget.
- **SPI** (Schedule Performance Index) = EV / PV.
- **CPI** (Cost Performance Index) = EV / AC.
- **EAC** (Estimate at Completion) = BAC / CPI (one common formula).
- **ETC** (Estimate to Complete) = EAC − AC.
- **VAC** (Variance at Completion) = BAC − EAC.

These are simple formulas, but computing them correctly across a whole project (rolling task-level values up to the project) is real work and a great thing to demo.

Interview wording:

> Each task carries a budget and a percent complete. I roll task-level Earned Value up to the project and compute SPI and CPI, so the dashboard answers two questions a manager actually asks: are we behind, and are we over budget.

Recommended learning:

- Earned Value Management overview: https://en.wikipedia.org/wiki/Earned_value_management

### 1.7 Angular frontend

The frontend is where this project becomes visibly impressive: a live Gantt chart that recomputes.

What to know:

- TypeScript fundamentals: interfaces, generics, union types, `strict` mode,
- components, inputs/outputs, and template syntax,
- services and `HttpClient` for talking to the API,
- reactive state with **Signals** (Angular's modern default) and/or RxJS,
- routing for project / schedule / resources / budget views,
- a component library (Angular Material) for tables, dialogs, and forms.

Use Angular 22 (current stable as of June 2026) or Angular 21 (LTS). Modern Angular is **signals-first and zoneless by default** — lean into signals for state rather than older Zone.js patterns. Pair with Angular Material, and use a SignalStore (NgRx Signals) if the state grows complex.

Interview wording:

> The Angular app holds the schedule in a signal-based store. When the user adds a dependency, the app calls the API to recompute, and the Gantt and the critical-path highlight update reactively from the new state.

Recommended learning:

- Angular tutorial: https://angular.dev/tutorials/learn-angular
- Angular signals: https://angular.dev/guide/signals
- Angular Material: https://material.angular.io/

### 1.8 Data-dense visualization (Gantt and resource histogram)

This is the second technical heart: turning schedule data into the chart everyone recognizes.

What to know:

- a Gantt chart maps each task to a horizontal bar positioned by start date and sized by duration,
- a time axis (days/weeks) with a consistent pixel-per-day scale,
- dependency arrows connecting bars,
- critical-path tasks highlighted (typically red),
- a baseline bar drawn beneath the current bar to show drift,
- a **resource histogram**: a stacked bar per day showing each resource's allocation vs capacity, with over-allocation flagged.

Two realistic paths:

- **Build it yourself with SVG (or Canvas) + D3 scales.** Harder, but the strongest engineering story; you control everything and can explain every pixel.
- **Use a Gantt library** (e.g., the open-source Frappe Gantt, or a commercial one like Bryntum or DHTMLX). Faster, but more "glue code" than algorithm.

Recommendation:

- For the strongest story: render the Gantt yourself with SVG and D3 scales. It pairs naturally with the engine you built.
- If time is tight: use a library for the Gantt but **still build the resource histogram yourself**, so at least one custom visualization shows real skill.

Interview wording:

> I render the Gantt as SVG. I use a D3 time scale to map dates to x-pixels, draw each task as a rect, draw FS dependencies as elbow connectors, and colour zero-float tasks red. The baseline is a thin bar underneath so schedule drift is visible at a glance.

### 1.9 System design and separation of concerns

This is the skill an interviewer probes hardest, because it is the rarest in junior candidates.

What to know:

- the **domain/engine** layer must have zero dependency on ASP.NET, EF, or Angular,
- the **application** layer orchestrates use cases and is the only thing the API calls,
- the **infrastructure** layer (EF Core, file storage) is swappable,
- the **API** is a thin HTTP adapter,
- the **frontend** knows nothing about EF or the engine's internals — only the JSON contract.

Recommended layout:

```text
Angular SPA (Signals + Material)
    | HTTPS / JSON
    v
ASP.NET Core Web API  (controllers / DTOs / validation)
    |
    v
Application layer  (use-case services: AddTask, AddDependency, Recompute, Level, ComputeEVM)
    |
    +--> Domain / Scheduling Engine  (CPM forward+backward pass, float, leveling, EVM) -- NO framework deps
    |
    +--> Infrastructure  (EF Core DbContext, repositories) --> SQL database
```

Interview wording:

> I deliberately kept the scheduling engine in a plain class library with no framework dependencies. That means I can unit-test the CPM algorithm in milliseconds with no database, and I could swap EF for Dapper or the API for gRPC without touching the engine.

### 1.10 DevOps, cloud, and deployment

What to know:

- Git and GitHub with a sensible commit history,
- `.env` / user-secrets for connection strings and keys (never commit secrets),
- a `Dockerfile` for the API and a multi-stage build for the Angular app,
- `docker compose` to run API + database + frontend locally,
- a CI workflow (GitHub Actions) that builds, tests, and lints on every push,
- deployment to a cloud platform with a managed database.

Cloud choices:

- **Azure** is the natural fit for .NET: App Service (or Container Apps) for the API, Azure SQL for the database, Static Web Apps for the Angular frontend.
- **AWS** works too: ECS/Fargate or Elastic Beanstalk for the API, RDS for the database, S3 + CloudFront for the frontend.

Recommendation: Azure, because the .NET tooling and free tiers make the deploy story shortest. Document the cost at low usage.

### 1.11 Software engineering practices

The job description still asks for maintainable code, tests, and team standards.

Minimum project engineering standard:

```text
solution/
  README.md
  ARCHITECTURE.md
  docker-compose.yml
  .gitignore
  src/
    Domain/
    Application/
    Infrastructure/
    Api/
  web/                # Angular app
  tests/
    Domain.Tests/     # CPM correctness tests live here
    Api.Tests/
  docs/
    architecture.png
```

Resume phrase after completing the project:

> Designed and deployed a full-stack project-scheduling application using ASP.NET Core and Angular, implementing a Critical Path Method engine, resource-leveling heuristics, and Earned Value cost tracking as an independently unit-tested domain layer behind a typed REST API and a reactive Gantt UI.

## 2. What Project Scheduling Is and Why It Matters

Project scheduling is the discipline behind tools like Oracle Primavera P6 and Microsoft Project. The idea is simple, the engineering is not.

Plain explanation:

- a project is a set of tasks, each with a duration,
- tasks depend on each other ("you can't pour the foundation before digging"),
- given those durations and dependencies, the schedule answers: when does the project finish, and which tasks can't slip?
- assign people and money to those tasks, and you also answer: who's overloaded, and are we over budget?

Why this is a hard, interesting problem:

- the dependency network is a graph, so naive approaches break on cycles, diamonds, and long chains,
- the "critical path" is non-obvious in any real project and changes whenever a duration or dependency changes,
- resource leveling is genuinely NP-hard, so honesty about heuristics matters,
- the data is dense and only makes sense visually, which forces real frontend work.

Why companies care:

- scheduling tools are core to construction, engineering, defense, energy, and any capital-project industry,
- they are also a clean showcase of full-stack ability: a non-trivial backend algorithm exposed through an API to a demanding UI.

Important framing:

> A scheduling app is not a todo list with dates. The value is in the computation: the critical path, the float, the resource conflicts, the cost variance. The CRUD is the easy 20 percent.

Do not say:

> I built a project management tool like MS Project.

Do say:

> I built the core scheduling engine of a tool like MS Project — critical path, float, resource leveling, and earned value — and a reactive Gantt UI on top of it, scoped honestly.

## 3. Reference Resources and Projects

### 3.1 Official documentation

- ASP.NET Core Web API: https://learn.microsoft.com/en-us/aspnet/core/web-api/
- EF Core: https://learn.microsoft.com/en-us/ef/core/
- C# language reference: https://learn.microsoft.com/en-us/dotnet/csharp/
- Angular docs: https://angular.dev/
- Angular Material: https://material.angular.io/
- D3 (for custom visualization): https://d3js.org/

### 3.2 Domain references

- Critical Path Method: https://en.wikipedia.org/wiki/Critical_path_method
- Topological sorting: https://en.wikipedia.org/wiki/Topological_sorting
- Earned Value Management: https://en.wikipedia.org/wiki/Earned_value_management
- Work Breakdown Structure: https://en.wikipedia.org/wiki/Work_breakdown_structure

### 3.3 Reference projects to study

Read these for patterns, not to copy.

- A clean ASP.NET Core layered/"clean architecture" template (search GitHub for " asp.net core clean architecture template") — study the layer boundaries.
- An open-source Gantt component such as Frappe Gantt (https://github.com/frappe/gantt) — study how it maps dates to pixels and draws dependencies.
- Any open-source CPM implementation — study how it handles topological order and cycle detection, then write your own.

How to use them:

- Read the layer boundaries and the folder structure.
- Read how the Gantt maps time to pixels.
- Do not copy and submit. Reviewers spot a copied template instantly. Build your own engine; that is the whole point.

### 3.4 Validation references

- Microsoft Project or a free trial of Oracle P6, or any online CPM calculator, used purely to **check your engine's output** against a known-good tool.
- Hand-worked CPM examples (there are many in PM textbooks and online) with published ES/EF/LS/LF/float values.

You don't need to own these tools, but you should know they're the ground truth your engine is measured against.

## 4. Proposed Project

### 4.1 Project title

Project Scheduling & Resource Management App

### 4.2 One-sentence pitch

> I built a full-stack scheduling app on ASP.NET Core and Angular where users build a task network and the system computes the critical path and float, levels resources against capacity, tracks progress against a baseline, and reports Earned Value cost metrics on a live Gantt and resource histogram.

### 4.3 Business problem

Any team running a multi-step project with dependencies and shared people hits the same problems:

- no one knows which tasks actually drive the end date (the critical path),
- people get double-booked because allocations aren't visible,
- progress is tracked in a spreadsheet that can't tell you if you're behind or over budget,
- a delay in one task silently ripples through the plan with no recomputation.

A scheduling app answers these directly and is a real workplace problem, not just a classroom demo.

### 4.4 The system

End-to-end flow:

1. User creates a project with a start date and a working calendar.
2. User adds tasks (a Work Breakdown Structure) with durations and percent-complete.
3. User draws dependencies between tasks (FS, SS, FF, SF with optional lag).
4. The backend runs the CPM engine: forward pass, backward pass, float, critical path. Cycles are rejected.
5. User assigns resources to tasks; the system flags over-allocation and can level.
6. User captures a baseline; later, drift from baseline is shown on the Gantt.
7. The budget view rolls up Earned Value and reports SPI, CPI, EAC, and VAC.
8. The frontend renders a live Gantt (with critical-path highlight and baseline bars) and a resource histogram.

### 4.5 Baseline (validate the engine before trusting it)

You must validate the engine first. This is the scheduling-app equivalent of "build a baseline before trusting your model."

Validation strategy:

- **Hand-worked check**: encode the worked example from section 1.4 (and 3–4 more from textbooks/online with published float values) as unit tests. Your engine must reproduce ES/EF/LS/LF/float and the correct critical path exactly.
- **Tool cross-check**: build the same small network in Microsoft Project, Oracle P6, or an online CPM calculator, and confirm your finish date, float values, and critical path match.

Why this matters:

A scheduling engine that "looks plausible" is worthless. The whole product is correctness. Without validation against known-good results, "the Gantt looks right" is wishful thinking, not engineering.

Interview wording:

> Before I built any UI, I validated the engine against hand-worked CPM examples and cross-checked one network in Microsoft Project. My float values and critical path matched, so I knew the rest of the app was standing on correct math.

### 4.6 Components and tech stack

Recommended stack (interview-friendly path):

| Layer | Choice | Why |
|------|--------|-----|
| Frontend | Angular 22 (or 21 LTS) + Material | Strong typed SPA framework, signals-first |
| Charts | Custom SVG + D3 scales | Best engineering story; full control of the Gantt |
| API | ASP.NET Core 10 Web API | Standard, fast, great tooling, LTS |
| Domain engine | Plain C# class library | Framework-free, unit-testable CPM/EVM core |
| ORM | Entity Framework Core | Standard .NET data access |
| Database | SQL Server or PostgreSQL | SQL Server for Azure ease; Postgres for cross-platform |
| Real-time (optional) | SignalR | Live multi-user schedule updates |
| Compute | Azure App Service / Container Apps | Shortest .NET deploy story |
| DB hosting | Azure SQL (or RDS Postgres) | Managed, low-effort |
| Containers | Docker + docker compose | Reproducible local + deploy |
| CI | GitHub Actions | Build, test, lint on every push |

You must be able to defend each choice with a one-sentence tradeoff.

### 4.7 The scheduling engine (domain core)

This is the part that earns the project. Implement it in a framework-free class library so it can be unit-tested without a database.

Algorithm steps:

1. Load the task graph into memory (nodes = tasks, edges = dependencies).
2. **Detect cycles**; if any exist, reject the schedule with a clear error (a cyclic plan is invalid).
3. **Topologically sort** the tasks.
4. **Forward pass** in topological order: compute ES/EF for each task from its predecessors' constraints, honouring relationship type and lag, and the project calendar (skip non-working days).
5. **Backward pass** in reverse order: compute LS/LF from successors.
6. Compute **total float** and **free float**; flag zero-total-float tasks as **critical**.
7. Persist the computed dates back to the tasks in a single transaction.

Hard requirements:

- support FS at minimum; ideally all four relationship types with lag,
- respect a working-day calendar (no work on weekends/holidays),
- reject cycles explicitly,
- be deterministic: same input always yields the same schedule.

Interview wording:

> The engine is pure C# with no framework dependencies. It topologically sorts the network, runs forward and backward passes that honour relationship types, lag, and a working calendar, derives float, and flags the critical path. Cyclic networks are rejected before any computation.

### 4.8 Resource and cost engines

Resource leveling (heuristic):

- detect over-allocation by summing assigned units per resource per day,
- apply a priority-rule heuristic (lowest float first) that delays tasks until resource capacity is free,
- recompute the schedule and report the resulting project delay,
- be explicit that this is a heuristic, not an optimizer.

Cost / Earned Value:

- each task has a budget and a percent complete,
- compute task-level EV, PV, AC and roll up to the project,
- report SV, CV, SPI, CPI, EAC, ETC, VAC,
- surface these on a dashboard with simple status colours (e.g., red when SPI or CPI < 1).

### 4.9 Evaluation (correctness and performance)

This is what separates a serious project from a toy demo.

Correctness:

- a suite of CPM unit tests using hand-worked networks with known float values,
- tests for each relationship type (FS/SS/FF/SF) and lag,
- a cycle-detection test (a cyclic network must be rejected),
- a calendar test (a task spanning a weekend lands on the correct working days),
- an EVM test with hand-computed SPI/CPI.

Performance:

- benchmark scheduling a generated network of increasing size (e.g., 100, 1,000, 10,000 tasks),
- report the recompute time and confirm it scales roughly linearly with tasks + dependencies,
- note where it would stop scaling and why.

Metrics to report in the README:

- engine correctness: number of CPM/EVM tests passing,
- recompute latency for N tasks (p50/p95),
- leveling: example project finish before vs after leveling,
- end-to-end latency from "add dependency" to "Gantt updated."

Interview wording:

> I have around 30 engine tests covering every relationship type, lag, cycles, and calendars. Recomputing a 1,000-task network takes about X milliseconds and scales linearly. I also show one network where leveling pushes the finish out by N days to remove an over-allocation.

(Use real numbers from your actual runs.)

### 4.10 Repository structure

```text
scheduling-app/
  README.md
  ARCHITECTURE.md
  docker-compose.yml
  .gitignore
  src/
    Domain/
      Entities/            # Task, Dependency, Resource, Assignment, Calendar
      Scheduling/
        CpmEngine.cs       # forward/backward pass, float, critical path
        TopologicalSort.cs
        CycleDetector.cs
        ResourceLeveler.cs
      Cost/
        EvmCalculator.cs
    Application/
      Projects/
      Scheduling/          # AddTask, AddDependency, RecomputeSchedule, LevelResources
      Cost/
    Infrastructure/
      Persistence/
        SchedulingDbContext.cs
        Configurations/
        Migrations/
      Repositories/
    Api/
      Controllers/
      Dtos/
      Program.cs
  web/                     # Angular app
    src/app/
      schedule/            # Gantt + critical path
      resources/           # histogram + leveling
      budget/              # EVM dashboard
      core/                # api services, signal stores, models
  tests/
    Domain.Tests/
      CpmEngineTests.cs    # the worked examples live here
      ResourceLevelerTests.cs
      EvmCalculatorTests.cs
    Api.Tests/
  docs/
    architecture.png
```

### 4.11 Architecture diagram

This is a non-negotiable deliverable.

Required elements:

- Angular SPA box (Gantt, resources, budget views),
- ASP.NET Core API box (controllers / DTOs),
- Application layer box,
- Domain/Scheduling Engine box (clearly framework-free),
- Infrastructure + database box,
- arrows showing the request flow (UI → API → application → engine + persistence → back),
- a callout that the engine has no framework dependencies.

Tools:

- Excalidraw (https://excalidraw.com) for a clean hand-drawn look,
- draw.io / diagrams.net,
- Mermaid (renders directly in GitHub READMEs).

Embed the diagram at the top of the README.

### 4.12 Minimum deliverables

Don't show up with only a backend or only a UI.

Minimum:

- public GitHub repository with a sensible commit history,
- README with architecture diagram and a metrics table,
- working local run via `docker compose up`,
- a working CPM engine with a passing test suite,
- a Gantt view that highlights the critical path,
- a deployed API + frontend (even on free tiers),
- a short demo script or screen recording: add tasks, draw a dependency, watch the critical path recompute.

Better:

- all four relationship types with lag,
- resource histogram with over-allocation flags and a working leveler,
- baseline capture and drift display on the Gantt,
- EVM dashboard,
- GitHub Actions running build + tests + lint.

Stretch:

- real-time multi-user updates with SignalR,
- multiple projects / portfolio view,
- export to a recognized format (e.g., XML import/export compatible with MS Project),
- authentication and per-user project isolation.

### 4.13 README outline

```markdown
# Project Scheduling & Resource Management App

## Problem
Teams running dependent tasks with shared people can't see what drives the end
date, who's overloaded, or whether they're over budget. This app builds a task
network, computes the critical path and float, levels resources, and reports
Earned Value cost metrics on a live Gantt.

## Architecture
![diagram](docs/architecture.png)

Angular SPA -> ASP.NET Core API -> Application layer -> framework-free
Scheduling Engine + EF Core persistence -> SQL database.

## Stack
- Frontend: Angular 22 + Material, custom SVG/D3 Gantt
- API: ASP.NET Core 10 Web API
- Engine: plain C# class library (CPM, leveling, EVM)
- Data: EF Core + Azure SQL
- Infra: Docker, GitHub Actions, Azure

## Engine correctness
- 30 unit tests covering FS/SS/FF/SF, lag, cycles, calendars, EVM: all passing
- Validated against Microsoft Project on a sample network (matching float + critical path)

## Performance
- Recompute 1,000-task network: ~X ms (scales ~linearly)

## Limitations
- Resource leveling is a heuristic, not an optimizer.
- Not a P6/MS Project clone; core engine only.
- Single-project demo unless auth is added.

## How to Run
Local: docker compose up
Deploy: ...
```

## 5. Six-Week Work Plan

### Week 1: The engine, in isolation

Goal:

- build the framework-free `Domain` library: entities, topological sort, cycle detection, CPM forward/backward pass, float.
- no API, no database, no UI yet.

Deliverables:

- `CpmEngine` that reproduces the worked example in section 1.4,
- a `Domain.Tests` project with hand-worked networks passing,
- cross-check one network against MS Project or an online CPM calculator.

Must be able to explain:

- what a topological sort is and why CPM needs it,
- what total float and the critical path mean,
- why a cyclic network is invalid.

### Week 2: Persistence and the API

Goal:

- model the schema in EF Core, add migrations,
- build the application layer (AddTask, AddDependency, RecomputeSchedule),
- expose it through an ASP.NET Core Web API with DTOs and validation,
- add Swagger.

Deliverables:

- working API you can drive from Swagger to build a small schedule,
- a database that persists tasks, dependencies, and computed dates,
- first architecture diagram draft.

Must be able to explain:

- why the engine has no EF dependency,
- how a graph persists in relational tables,
- where validation lives and why.

### Week 3: The Angular app and the Gantt

Goal:

- scaffold the Angular app with Material and a signal-based store,
- build the task list and dependency editor,
- render the Gantt (SVG + D3 scales) with critical-path highlight,
- wire "add dependency" to recompute and re-render.

Deliverables:

- a working local web app: build a schedule, see the critical path,
- the live recompute demo working end-to-end.

Must be able to explain:

- how dates map to pixels in the Gantt,
- how the frontend stays in sync after a recompute,
- why the frontend doesn't know the engine internals.

### Week 4: Resources and leveling

Goal:

- add resources, assignments, and a resource histogram,
- detect over-allocation,
- implement the priority-rule leveling heuristic.

Deliverables:

- resource histogram with over-allocation flags,
- a working leveler with a before/after demo,
- tests for the leveler.

Must be able to explain:

- why leveling is NP-hard,
- what your heuristic does and where it's suboptimal.

### Week 5: Budget, baseline, and EVM

Goal:

- add task budgets and percent-complete,
- capture a baseline and show drift on the Gantt,
- build the EVM dashboard (SPI, CPI, EAC, VAC).

Deliverables:

- baseline capture + drift display,
- EVM dashboard with status colours,
- EVM unit tests.

Must be able to explain:

- the difference between PV, EV, and AC,
- what SPI and CPI tell a manager.

### Week 6: Deploy, harden, and polish

Goal:

- Dockerize, add docker compose, add GitHub Actions,
- deploy API + DB + frontend to the cloud,
- write the README with the metrics table and record a demo.

Deliverables:

- deployed public demo,
- CI running build + tests,
- README + architecture diagram + short demo video.

Must be able to explain:

- your deployment topology and rough cost,
- how secrets/connection strings are handled,
- where the system would fail at scale.

## 5A. Compressed 2-Week Version

Use this if a deadline is close. Cut leveling, EVM, and baselines first; protect the engine and the Gantt.

### Days 1-3: engine + tests

- build the CPM engine (FS only is acceptable here) with topological sort and cycle detection,
- get the worked example and 2–3 more passing as tests,
- first commit on GitHub.

### Days 4-6: API + persistence

- EF Core schema + migrations,
- application services + Web API + Swagger,
- build a small schedule end-to-end through the API.

### Days 7-10: Angular + Gantt

- Angular app with task list, dependency editor, and an SVG Gantt,
- critical-path highlight and live recompute,
- a minimal resource list (assignments only, no leveling).

### Days 11-12: deploy

- Docker + docker compose,
- deploy API + DB + frontend to the cloud,
- redeploy and test from the internet.

### Days 13-14: polish + rehearsal

- README with architecture diagram and a small metrics table,
- 90-second demo recording: add tasks, draw a dependency, critical path moves,
- rehearse the talk track and the validation questions in section 8.

If time runs out, ship FS-only with a correct critical path and a clean Gantt. A correct, deployed, well-explained engine beats a half-built leveler.

## 6. Demo / Interview Talk Track

### 30-second version

> I built a full-stack project-scheduling app on ASP.NET Core and Angular. Users build a task network, and a framework-free C# engine computes the critical path and float with a forward and backward pass, rejecting cyclic plans. The Angular frontend renders a live SVG Gantt that highlights the critical path and updates the moment a dependency changes. I added resource leveling and Earned Value cost tracking on top, and validated the engine against Microsoft Project.

### 2-minute version

> The system has a clear spine. The Angular SPA talks to an ASP.NET Core API over JSON. The API is a thin adapter: it validates DTOs and calls application services. Those services orchestrate a domain layer that has no framework dependencies, which is deliberate — it means I can unit-test the scheduling algorithm in milliseconds with no database.
>
> The engine itself loads the dependency network as a graph, detects cycles and rejects invalid plans, topologically sorts the tasks, then runs a forward pass for early dates and a backward pass for late dates, honouring relationship types, lag, and a working-day calendar. Total float falls out of the two passes, and zero-float tasks are the critical path. I validated all of this against hand-worked examples and one network cross-checked in Microsoft Project.
>
> On top of the engine I built a resource layer that detects over-allocation and levels with a lowest-float-first heuristic — I'm explicit that leveling is NP-hard and this isn't optimal — and an Earned Value layer that rolls task budgets and percent-complete up into SPI and CPI. The frontend renders the schedule as SVG with a D3 time scale, draws dependency connectors, highlights the critical path in red, and shows a baseline bar so drift is visible. The biggest risk in a tool like this is silent incorrectness, which is exactly why I led with a test suite instead of with the UI.

### Strong points

- "I built the engine in isolation and validated it before writing any UI."
- "The scheduling engine has zero framework dependencies and is unit-tested without a database."
- "I reject cyclic networks explicitly instead of looping forever."
- "I'm honest that resource leveling is a heuristic, not an optimizer."
- "I cross-checked my float and critical path against Microsoft Project."
- "I can explain where it stops scaling and why."

### Things not to claim

- Do not claim you cloned MS Project or Oracle P6.
- Do not claim your resource leveler is optimal.
- Do not claim production readiness without auth, authorization, and observability.
- Do not claim it handles every P6 edge case (calendars, constraints, and resource curves get deep fast).
- Do not claim you invented CPM; it's a documented 1950s method you implemented correctly.

## 7. Resume Skill Mapping

### Skill section after completing the project

```text
Languages: C#, TypeScript, SQL
Backend: ASP.NET Core Web API, Entity Framework Core, clean/layered architecture, DI
Frontend: Angular, Angular Material, RxJS/Signals, SVG/D3 data visualization
Algorithms: graph algorithms, topological sort, Critical Path Method, heuristic scheduling
Domain: project scheduling, resource leveling, Earned Value Management
Engineering: Docker, GitHub Actions, unit testing, REST API design
Cloud: Azure App Service / Azure SQL (or AWS equivalents)
```

### Project bullet examples

Use only bullets that are true.

```text
- Designed and deployed a full-stack project-scheduling application (ASP.NET Core + Angular) implementing a Critical Path Method engine with forward/backward passes, float, cycle detection, and working-day calendars.
- Built the scheduling engine as a framework-free C# library covered by ~30 unit tests, validated against Microsoft Project; recomputes a 1,000-task network in ~X ms with linear scaling.
- Implemented a priority-rule resource-leveling heuristic and an Earned Value cost module (SPI, CPI, EAC, VAC) rolled up from task-level budgets and progress.
- Developed a reactive Angular UI with a custom SVG/D3 Gantt highlighting the critical path and baseline drift, plus a resource-allocation histogram with over-allocation detection.
- Containerized the stack with Docker, set up GitHub Actions CI, and deployed the API, database, and SPA to Azure.
```

## 8. How to Validate Whether You Really Understand It

If you can't answer these clearly, you're not done.

### Scheduling concept questions

1. What is the critical path and why does it matter?
2. What is total float, and how is it different from free float?
3. Why does CPM need a topological sort?
4. What happens if the dependency network has a cycle?
5. What's the difference between Finish-to-Start and Start-to-Start, and when would you use SS?
6. What does lag do, and what's a lead?

### Algorithm questions

1. Walk me through your forward pass and backward pass.
2. How do you detect a cycle in the dependency graph?
3. How does your engine handle a working calendar with weekends and holidays?
4. What's the time complexity of one recompute?
5. How would you recompute incrementally instead of from scratch?

### Resource and cost questions

1. What is resource over-allocation and how do you detect it?
2. Why is resource leveling NP-hard, and what heuristic did you use?
3. Show me a case where your leveler is suboptimal.
4. What's the difference between Planned Value, Earned Value, and Actual Cost?
5. If SPI is 0.8 and CPI is 1.1, what's the project's situation?

### System design questions

1. Why does your scheduling engine have no framework dependencies?
2. How does a dependency graph persist in relational tables?
3. Where does validation live, and why not in the engine?
4. How would you support 10,000 tasks? 100 concurrent users?
5. How would you make recompute real-time for multiple editors?
6. Where could a connection string or secret leak, and how did you prevent it?

### Engineering questions

1. How do I run your project from a fresh laptop?
2. What does your "add dependency" request do end to end?
3. How did you validate the engine is correct?
4. What's your recompute latency, and where is the time spent?
5. What breaks first as the project grows, and how would you fix it?

## 9. Reference Pack

### Official documentation

- ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/
- ASP.NET Core Web API tutorial: https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api
- Minimal APIs: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/overview
- Entity Framework Core: https://learn.microsoft.com/en-us/ef/core/
- C# guide: https://learn.microsoft.com/en-us/dotnet/csharp/
- Angular: https://angular.dev/
- Angular signals: https://angular.dev/guide/signals
- Angular Material: https://material.angular.io/
- D3: https://d3js.org/

### Cloud and DevOps documentation

- Azure App Service: https://learn.microsoft.com/en-us/azure/app-service/
- Azure Container Apps: https://learn.microsoft.com/en-us/azure/container-apps/
- Azure SQL Database: https://learn.microsoft.com/en-us/azure/azure-sql/database/
- Azure Static Web Apps: https://learn.microsoft.com/en-us/azure/static-web-apps/
- GitHub Actions: https://docs.github.com/en/actions
- Docker get started: https://docs.docker.com/get-started/

### Domain references

- Critical Path Method: https://en.wikipedia.org/wiki/Critical_path_method
- Topological sorting: https://en.wikipedia.org/wiki/Topological_sorting
- Earned Value Management: https://en.wikipedia.org/wiki/Earned_value_management
- Work Breakdown Structure: https://en.wikipedia.org/wiki/Work_breakdown_structure

### Reference repositories to study

Use these as workflow references, not as code to copy.

- An ASP.NET Core clean-architecture template (search GitHub) — for layer boundaries.
- Frappe Gantt: https://github.com/frappe/gantt — for how a Gantt maps time to pixels and draws dependencies.
- Any open-source CPM implementation — for topological order and cycle handling.

When studying reference repos, focus on:

- layer boundaries and module structure,
- how the graph is loaded and traversed,
- how the Gantt maps dates to pixels and renders dependencies,
- how errors (cycles, over-allocation) are handled,
- not the surface-level UI.

## 10. Final Recommendation

Build this project in the order the work plan lays out: **engine first, UI last.** The single most common failure mode is starting with a pretty Gantt on top of an engine that computes the wrong float. Lead with correctness.

```text
Scheduling Engine (CPM: topological sort, forward/backward pass, float, cycles)
   |
   v
API + Persistence (ASP.NET Core, EF Core, clean layer boundaries)
   |
   v
Angular UI (SVG/D3 Gantt, critical path, resource histogram)
   |
   v
Resources + Cost (leveling heuristic, Earned Value) + Cloud deploy
```

Together these cover the two halves of a modern full-stack .NET engineering JD:

- the backend + algorithms half (C#, ASP.NET Core, EF Core, graph algorithms, domain modeling),
- the frontend + system-design + cloud half (Angular, data-dense visualization, layered architecture, deployment).

The project should be judged by whether you can explain:

- what the critical path and float are, and how your engine computes them,
- why the engine is kept independent of the framework, and how you tested it,
- how you validated correctness against a known-good tool,
- how the schedule graph persists and recomputes,
- where the system fails (leveling optimality, scale) and why you're honest about it,
- how another engineer would run and extend it.

If you can build and explain this well, you demonstrate exactly the high-value skills that a CRUD app never shows: real algorithms, clean architecture, demanding visualization, and the engineering discipline to validate correctness before trusting the output.
