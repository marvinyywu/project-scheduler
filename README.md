# Project Scheduling & Resource Management App

## Problem

Teams running dependent tasks with shared people can't see what drives the
end date, who's overloaded, or whether they're over budget. This app builds
a task network, computes the critical path and float, levels resources, and
reports Earned Value cost metrics on a live Gantt.

Full project scope and rationale: [`project-scheduler/Project_Scheduling_App_Project_Pack.md`](project-scheduler/Project_Scheduling_App_Project_Pack.md).

## Architecture

```text
Angular SPA (not yet built)
    | HTTPS / JSON
    v
ASP.NET Core Web API (not yet wired to the engine)
    |
    v
Application layer (not yet built)
    |
    +--> Domain / Scheduling Engine  (CPM forward+backward pass, float) -- NO framework deps
    |
    +--> Infrastructure / EF Core (not yet built)
```

The scheduling engine (`src/Domain`) is a plain C# class library with zero
framework dependencies, so it can be unit-tested without a database, API, or
UI.

## Stack

- Engine: plain C# class library (`src/Domain`) — CPM today; leveling and EVM planned
- Tests: xUnit (`tests/Domain.Tests`)
- API: ASP.NET Core Web API (`project-scheduler/`) — currently the default template, not yet wired to the engine
- Frontend: Angular — not started
- Data: EF Core + SQL — not started

## Status: Week 1 (engine, in isolation)

Built so far:

- `Task`/`Dependency` entities (`src/Domain/Entities`)
- Cycle detection, topological sort, and a CPM engine with forward pass,
  backward pass, total float, free float, and critical-path flagging,
  supporting all four relationship types (FS/SS/FF/SF) with lag
  (`src/Domain/Scheduling`)
- `Domain.Tests` reproducing the hand-worked example from the project pack
  (section 1.4), a cycle-rejection test, and a relationship-type/lag test —
  all passing

Not yet built: working-day calendars, persistence, the API, the frontend,
resource leveling, EVM, baselines, and deployment. See the six-week plan in
the project pack for what's next.

## How to Run

```bash
dotnet test tests/Domain.Tests/Domain.Tests.csproj
```

There is no database, API, or UI to run yet.
