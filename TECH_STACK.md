# Tech Stack Documentation

## Overview
This document describes the technical stack used in the Self-Finance Manager Backend (WebApiForAz) solution.

## Core Technologies

### .NET Framework
- **.NET 9.0** - Latest version of the .NET platform
- **ASP.NET Core** - Web API framework
- **C#** - Programming language with nullable reference types enabled
- **Implicit Usings** - Enabled for cleaner code

### Database Technologies
- **PostgreSQL** - Primary production database
- **SQL Server** - Alternative database support (configured via Entity Framework)
- **Entity Framework Core 9.0.8** - ORM (Object-Relational Mapping)
  - Code-First approach with migrations
  - `EFCore.NamingConventions` (9.0.0) - Snake case naming conventions for PostgreSQL
  - `Npgsql.EntityFrameworkCore.PostgreSQL` (9.0.4) - PostgreSQL provider
  - `Microsoft.EntityFrameworkCore.SqlServer` (9.0.8) - SQL Server provider
  - `Microsoft.EntityFrameworkCore.InMemory` (9.0.11) - In-memory database for testing (uses latest patch version)

## Authentication & Authorization

### Identity Framework
- **ASP.NET Core Identity** (9.0.0) - User management and authentication
  - Extended with custom `ApplicationUser` entity
  - Role-based access control (Admin, User roles)
  - Password policy enforcement

### Token-Based Authentication
- **JWT (JSON Web Tokens)** - Stateless authentication
  - `Microsoft.AspNetCore.Authentication.JwtBearer` (9.0.0)
  - `System.IdentityModel.Tokens.Jwt` (8.0.1)
  - Configurable token expiry (default: 24 hours)
  - HMAC-SHA256 signing algorithm

## API Documentation

### OpenAPI/Swagger
- **Swashbuckle.AspNetCore** (9.0.3) - Swagger UI and OpenAPI specification
- **Microsoft.AspNetCore.OpenApi** (9.0.6) - OpenAPI support
- XML documentation generation enabled for API endpoints
- Integrated JWT authentication in Swagger UI

## Logging

### Logging Framework
- **Serilog** (4.1.0) - Structured logging library
  - Used across all projects (DAL, BL, API)
  - Provides structured, queryable logs

## Testing

### Testing Frameworks
- **xUnit** (2.9.2) - Primary testing framework
  - `xunit.runner.visualstudio` (2.8.2) - Visual Studio test runner
- **Moq** (4.20.72) - Mocking framework for unit tests
- **coverlet.collector** (6.0.2) - Code coverage collection
- **Microsoft.NET.Test.Sdk** (17.12.0) - Test SDK

### Testing Strategy
- Unit tests for business logic services
- In-memory database for repository testing
- Mocking for external dependencies

## Development Tools

### Code Generation
- **Microsoft.VisualStudio.Web.CodeGeneration.Design** (9.0.0) - Scaffolding tools

### Entity Framework Tools
- **Microsoft.EntityFrameworkCore.Tools** (9.0.8) - CLI tools for migrations
- **Microsoft.EntityFrameworkCore.Design** (9.0.8) - Design-time support

## Configuration Management

### Configuration Sources
- **appsettings.json** - Base configuration
- **appsettings.Development.json** - Development-specific settings
- **Environment Variables** - Production configuration (preferred)
  - `DATABASE_URL` - Database connection string
  - `Jwt__Key` - JWT signing key
  - `Jwt__Issuer` - JWT issuer
  - `Jwt__Audience` - JWT audience
  - `Jwt__ExpiryInHours` - Token expiry time

### Configuration Packages
- `Microsoft.Extensions.Configuration` (9.0.0)
- `Microsoft.Extensions.Configuration.FileExtensions` (9.0.0)
- `Microsoft.Extensions.Configuration.Json` (9.0.0)

## Containerization

### Docker
- **Multi-stage Dockerfile**
  - Build stage: `mcr.microsoft.com/dotnet/sdk:9.0`
  - Runtime stage: `mcr.microsoft.com/dotnet/aspnet:9.0`
- Optimized for production deployment
- Exposes port 80
- Environment variable support

## Project Architecture

### Solution Structure
The solution follows a layered architecture pattern:

```
WebApiForAz.sln
│
├── WebApiForAz (API Layer)
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── OperationsController.cs
│   │   ├── OperationTypesController.cs
│   │   ├── DailyReportController.cs
│   │   ├── PeriodReportController.cs
│   │   └── UserManagementController.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Program.cs
│   └── WebApiForAz.csproj
│
├── SFMB.BL (Business Logic Layer)
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── OperationService.cs
│   │   ├── OperationTypeService.cs
│   │   ├── DailyReportService.cs
│   │   └── PeriodReportService.cs
│   ├── Dtos/
│   │   ├── LoginDto.cs
│   │   ├── RegisterDto.cs
│   │   └── AuthResponseDto.cs
│   └── SFMB.BL.csproj
│
├── SFMB.DAL (Data Access Layer)
│   ├── Entities/
│   │   ├── ApplicationUser.cs
│   │   ├── Operation.cs
│   │   └── OperationType.cs
│   ├── Repositories/
│   │   ├── OperationRepository.cs
│   │   ├── OperationTypeRepository.cs
│   │   ├── DailyReportRepository.cs
│   │   └── PeriodReportRepository.cs
│   ├── Migrations/
│   ├── SfmbDbContext.cs
│   ├── DbSeeder.cs
│   └── SFMB.DAL.csproj
│
└── FSMB.UnitTests (Testing Project)
    ├── AuthServiceTests.cs
    └── FSMB.UnitTests.csproj
```

### Design Patterns
- **Repository Pattern** - Data access abstraction
- **Dependency Injection** - Built-in ASP.NET Core DI container
- **Service Layer Pattern** - Business logic separation
- **DTO Pattern** - Data transfer objects for API contracts
- **Middleware Pattern** - Exception handling and request processing

## Security Features

### Built-in Security
- **HTTPS Redirection** - Enforced in production
- **JWT Token Validation** - On every authenticated request
- **Role-Based Authorization** - User and Admin roles
- **Password Policy** - Enforced complexity requirements
- **Data Ownership** - Users can only access their own data
- **Admin Privileges** - Special access for administrative tasks

### Security Best Practices
- Environment variables for sensitive configuration
- No hardcoded secrets in source code
- Token expiry management
- Validation of all user inputs
- Exception handling middleware

## Key NuGet Packages Summary

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.AspNetCore.Authentication.JwtBearer | 9.0.0 | JWT authentication |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 9.0.0 | Identity with EF Core |
| Microsoft.EntityFrameworkCore | 9.0.8 | ORM framework |
| Npgsql.EntityFrameworkCore.PostgreSQL | 9.0.4 | PostgreSQL provider |
| Swashbuckle.AspNetCore | 9.0.3 | Swagger/OpenAPI |
| Serilog | 4.1.0 | Structured logging |
| xUnit | 2.9.2 | Testing framework |
| Moq | 4.20.72 | Mocking framework |
| EFCore.NamingConventions | 9.0.0 | Snake case naming |

## Deployment Targets

### Supported Platforms
- **DigitalOcean** - Primary deployment target (via Docker)
- **Azure App Service** - Alternative cloud platform
- **Any Docker-compatible hosting** - Via containerization
- **On-premise** - Traditional server deployment

### Deployment Methods
- Docker container deployment
- Direct .NET hosting
- Cloud platform-specific deployment

## Development Requirements

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL database (or SQL Server)
- Docker (optional, for containerized development)
- IDE: Visual Studio 2022, VS Code, or JetBrains Rider

### Build Commands
```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run application
dotnet run --project WebApiForAz

# Create migration
cd SFMB.DAL
dotnet ef migrations add MigrationName --startup-project ../WebApiForAz

# Apply migrations
dotnet ef database update --startup-project ../WebApiForAz

# Build Docker image
docker build -t webapi-for-az .

# Run Docker container
docker run -p 8080:80 webapi-for-az
```

## API Features

### REST API Capabilities
- CRUD operations for financial data
- User authentication and registration
- Role-based access control
- Daily financial reports
- Period-based financial reports
- User management (admin only)

### API Standards
- RESTful design principles
- JSON request/response format
- HTTP status codes (200, 201, 400, 401, 403, 404, 500)
- Bearer token authentication
- OpenAPI 3.0 specification

## Performance Considerations

### Optimization Features
- Entity Framework query optimization
- Async/await pattern throughout
- Connection pooling (via Npgsql)
- Lazy loading disabled (explicit eager loading)
- Indexed database columns for common queries

## Future Technology Considerations

### Potential Upgrades
- Redis for caching
- Message queues (RabbitMQ, Azure Service Bus)
- GraphQL endpoint alongside REST
- gRPC for internal service communication
- Azure Key Vault for secrets management
- Application Insights for monitoring
- Rate limiting middleware
- CORS configuration for frontend integration

## Version History

- **Current Version**: .NET 9.0
- **Entity Framework**: 9.0.8
- **Last Updated**: December 2024

## References

- [ASP.NET Core Documentation](https://learn.microsoft.com/aspnet/core)
- [Entity Framework Core Documentation](https://learn.microsoft.com/ef/core)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Swagger/OpenAPI Documentation](https://swagger.io/docs/)
- [JWT.io](https://jwt.io/)
