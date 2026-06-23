# 🏨 HotelBilling Pro — ASP.NET Core Web API

## Architecture
```
HotelBilling.sln
├── src/
│   ├── HotelBilling.Domain          ← Entities, Enums, BaseEntity
│   ├── HotelBilling.Application     ← CQRS (MediatR), Interfaces, Validation, Exceptions
│   ├── HotelBilling.Infrastructure  ← Dapper Repos, JWT, Serilog, SQL
│   └── HotelBilling.API             ← Controllers, Middleware, Program.cs
```

## Tech Stack
| Concern              | Technology                      |
|---------------------|---------------------------------|
| Framework            | ASP.NET Core 8 Web API          |
| Pattern              | Clean Architecture + CQRS       |
| Data Access          | Dapper (micro-ORM)              |
| Database             | SQL Server / Azure SQL          |
| Authentication       | JWT Bearer + Refresh Tokens     |
| Authorization        | Role-based (5 roles)            |
| Validation           | FluentValidation + MediatR pipe |
| Logging              | Serilog (Console + Rolling File)|
| API Docs             | Swagger / OpenAPI 3             |
| DI / Mediator        | MediatR 12                      |
| Password Hashing     | BCrypt.Net                      |
| Exception Handling   | Global middleware                |

## Quick Start

### 1. Database Setup
```sql
-- Run in SQL Server Management Studio / Azure Data Studio
-- File: src/HotelBilling.Infrastructure/Persistence/Scripts/001_CreateDatabase.sql
```

### 2. Configuration
Edit `src/HotelBilling.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=HotelBillingDB;..."
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyMinimum32Characters!"
  }
}
```

### 3. Run
```bash
cd src/HotelBilling.API
dotnet restore
dotnet run
# Swagger UI → http://localhost:5000
```

## Default Credentials (after running seed SQL)
| Role         | Email                          | Password   |
|-------------|-------------------------------|------------|
| Super Admin  | admin@hotelbilling.com         | Admin@123  |
| Front Desk   | frontdesk@hotelbilling.com     | Admin@123  |

## API Endpoints

### Auth  `POST /api/auth/...`
| Method | Endpoint               | Auth   | Description                  |
|--------|------------------------|--------|------------------------------|
| POST   | /login                 | Public | Email + password → JWT        |
| POST   | /register              | Public | Create new user account       |
| POST   | /refresh-token         | Public | Rotate expired access token   |
| POST   | /change-password       | ✅ JWT | Change own password           |
| POST   | /logout                | ✅ JWT | Invalidate refresh token      |
| GET    | /me                    | ✅ JWT | Get current user info         |

### Reservations  `GET|POST|PUT|DELETE /api/reservations/...`
| Method | Endpoint               | Role         | Description                    |
|--------|------------------------|--------------|--------------------------------|
| GET    | /                      | FrontDeskUp  | Paginated list + filters        |
| GET    | /{id}                  | FrontDeskUp  | Get by ID                       |
| POST   | /                      | FrontDeskUp  | Create + availability check     |
| PUT    | /{id}                  | FrontDeskUp  | Update                          |
| DELETE | /{id}                  | AdminOnly    | Soft delete                     |
| POST   | /{id}/check-in         | FrontDeskUp  | Check-in → Room = Occupied      |
| POST   | /{id}/check-out        | FrontDeskUp  | Check-out → Room = Dirty        |

### Invoices  `/api/invoices`
| GET / GET{id} / POST / PATCH{id}/status / DELETE{id} |

### Guests  `/api/guests`
| GET / GET{id} / POST / PUT{id} |

### Rooms  `/api/rooms`
| GET / POST / PATCH{id}/status |

### Dashboard  `GET /api/dashboard/stats`
Returns: today revenue, occupancy, active guests, pending invoices, revenue trend, occupancy trend

### Reports  `GET /api/reports?from=2026-01-01&to=2026-04-30`
Returns: revenue by channel, revenue by room type, KPIs (ADR, RevPAR, AvgStay, NoShowRate)

### Housekeeping  `/api/housekeeping/tasks`
| GET / POST / PATCH{id}/status |

## Role-Based Access Policy Matrix
| Policy          | Roles                                    |
|----------------|------------------------------------------|
| AdminOnly       | SuperAdmin, Admin                        |
| FrontDeskUp     | SuperAdmin, Admin, FrontDesk             |
| AccountsUp      | SuperAdmin, Admin, AccountsManager       |
| HousekeepingUp  | SuperAdmin, Admin, FrontDesk, Housekeeping|

## CQRS Structure (MediatR)
```
Features/
├── Auth/
│   └── Commands/  LoginCommand, RegisterCommand, RefreshTokenCommand,
│                  ChangePasswordCommand, LogoutCommand
├── Reservations/
│   ├── Commands/  CreateReservationCommand, UpdateReservationCommand,
│   │              DeleteReservationCommand, CheckInCommand, CheckOutCommand
│   └── Queries/   GetReservationsQuery, GetReservationByIdQuery
├── Invoices/
│   ├── Commands/  CreateInvoiceCommand, UpdateInvoiceStatusCommand
│   └── Queries/   GetInvoicesQuery, GetInvoiceByIdQuery
├── Guests/
│   ├── Commands/  CreateGuestCommand, UpdateGuestCommand
│   └── Queries/   GetGuestsQuery, GetGuestByIdQuery
├── Rooms/
│   ├── Commands/  CreateRoomCommand, UpdateRoomStatusCommand
│   └── Queries/   GetRoomsQuery
├── Dashboard/
│   └── Queries/   GetDashboardStatsQuery
├── Reports/
│   └── Queries/   GetReportQuery
└── Housekeeping/
    ├── Commands/  AssignHousekeepingTaskCommand, UpdateTaskStatusCommand
    └── Queries/   GetHousekeepingTasksQuery
```

## MediatR Pipeline Behaviors
1. **LoggingBehavior** — logs every request + response + errors via Serilog
2. **ValidationBehavior** — runs FluentValidation; throws `ValidationException` on failure → mapped to HTTP 422

## Serilog Output
- **Console** — colored structured logs during development
- **File** — rolling daily logs at `logs/hotelbilling-YYYYMMDD.log`, retained 30 days
- **Enrichers** — `MachineName`, `EnvironmentName`, `RequestId`, HTTP context
