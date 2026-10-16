# RaceDayAPI
# RaceDay API

![.NET CI](https://github.com/tshego2345678-tech/RaceDayAPi1/actions/workflows/dotnet-desktop.yml/badge.svg)

## System Description

RaceDay is a full-stack event management system for South African road running, walking and cycling events (for example Comrades and the Cape Town Cycle Tour). This repository contains the **Part 2 REST API**, built with C# and ASP.NET Core (.NET 8). It lets organisers create and manage events, and lets participants enter events and view their results.

## User Roles

**Organiser**
- Registers and logs in
- Creates, edits and deletes their own events
- Manages categories for their events (for example 10km, 21km, 42km)
- Views enrolments for their events
- Captures, updates and deletes results

**Participant**
- Registers and logs in
- Updates their own profile
- Browses events and enrols in them
- Views and cancels their own enrolments
- Views their own results

## Tech Stack

| Area | Technology |
|---|---|
| Framework | ASP.NET Core Web API (.NET 8) |
| Database | SQL Server with Entity Framework Core |
| Authentication | JWT Bearer tokens |
| API documentation | Swagger (Swashbuckle) |
| Testing | xUnit |
| CI/CD | GitHub Actions |

## Project Structure

```
RaceDayAPi1/
├─ .github/workflows/     CI/CD workflow
├─ RaceDayAPI/            API project (Controllers, Models, DTOs, Data, Migrations)
├─ RaceDayApi.Tests/      xUnit unit tests
├─ RaceDayAPI.sln         Solution file
└─ RaceDayMigration.sql   Database script
```

## API Endpoints

| Controller | Purpose | Access |
|---|---|---|
| Auth | Register participant, register organiser, login | Public |
| Profile | View and update own profile | Logged in |
| Events | List and view events; create, update and delete events | Read: public, Write: Organiser |
| Categories | Manage categories for an event | Organiser (event owner) |
| Enrolments | Enrol, view own enrolments, view event enrolments, cancel | Participant / Organiser |
| Results | Capture, update, delete and view results | Read: logged in, Write: Organiser |

The full list of routes, request bodies and responses is available in Swagger at `/swagger` when the API is running.

## Getting Started

**Requirements:** .NET 8 SDK and SQL Server (or SQL Server Express).

1. Clone the repository
   ```
   git clone https://github.com/tshego2345678-tech/RaceDayAPi1.git
   cd RaceDayAPi1
   ```
2. Set the connection string and JWT key in `RaceDayAPI/appsettings.json`
3. Create the database
   ```
   cd RaceDayAPI
   dotnet ef database update
   ```
   Alternatively, run `RaceDayMigration.sql` in SQL Server Management Studio.
4. Run the API
   ```
   dotnet run
   ```
5. Open `https://localhost:<port>/swagger` in your browser

## Running the Tests

```
dotnet test
```

## CI/CD

The GitHub Actions workflow restores, builds and tests the solution on every push.

![CI/CD green check](docs/ci-screenshot.png)

## Video Presentation

[Watch the Part 2 video on YouTube](PASTE-YOUR-UNLISTED-YOUTUBE-LINK-HERE)

## Author

Tshegofatso Modibela
