# Meal Preparation Service

A comprehensive ASP.NET Core Web API application for meal planning, virtual fridge management, and ingredient ordering with delivery tracking.

## Architecture

The application follows **N-Layer Architecture** with clear separation of concerns:

### Layer Structure

```
MealPreparationService/
├── src/
│   ├── MealPreparationService.API/          # Presentation Layer
│   │   ├── Controllers/                     # API Controllers
│   │   ├── Middleware/                      # Custom Middleware
│   │   └── Models/                          # DTOs and API Models
│   │
│   ├── MealPreparationService.Business/     # Business Logic Layer
│   │   └── Services/                        # Business Services
│   │
│   ├── MealPreparationService.DataAccess/   # Data Access Layer
│   │   ├── Data/                            # DbContext
│   │   ├── Repositories/                    # Repository Pattern
│   │   └── UnitOfWork/                      # Unit of Work Pattern
│   │
│   └── MealPreparationService.Domain/       # Domain Layer
│       └── Entities/                        # Domain Entities
```

## Technology Stack

- **Framework**: ASP.NET Core 9.0 Web API
- **Database**: Microsoft SQL Server 2019
- **ORM**: Entity Framework Core 9.0
- **Authentication**: JWT Bearer Tokens
- **Password Hashing**: BCrypt.Net
- **API Documentation**: Swagger/OpenAPI

## Features Implemented (Task 1)

✅ N-Layer architecture with 4 projects (Domain, DataAccess, Business, API)
✅ Entity Framework Core with SQL Server configuration
✅ Dependency Injection container with appropriate service lifetimes
✅ Global error handling middleware
✅ Request logging middleware with performance tracking
✅ JWT authentication configuration
✅ CORS configuration for frontend integration
✅ Security headers (X-Content-Type-Options, X-Frame-Options, HSTS, etc.)
✅ Repository pattern with base repository
✅ Unit of Work pattern for transaction management
✅ Structured logging with configurable log levels
✅ Standardized API response format
✅ Password hashing service with BCrypt

## Configuration

### Database Connection

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=127.0.0.1;Database=MealPreparationService;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### JWT Settings

Configure JWT authentication in `appsettings.json`:

```json
{
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "MealPreparationService",
    "Audience": "MealPreparationServiceClient",
    "ExpirationHours": 24
  }
}
```

### CORS Origins

Configure allowed frontend origins:

```json
{
  "CorsOrigins": [
    "http://127.0.0.1:3000",
    "http://127.0.0.1:5173"
  ]
}
```

## Dependency Injection Lifetimes

### Scoped Services (per HTTP request)
- `ApplicationDbContext`
- `IUnitOfWork` / `UnitOfWork`
- `IUserRepository` / `UserRepository`
- `IPasswordHasher` / `PasswordHasher`

### Singleton Services (application lifetime)
- Configuration services
- Logging services

### Transient Services (per request)
- Validators (to be added)
- Mappers (to be added)

## Middleware Pipeline

1. **RequestLoggingMiddleware** - Logs all incoming requests with timing
2. **ErrorHandlingMiddleware** - Global exception handling
3. **Security Headers** - Adds security-related HTTP headers
4. **CORS** - Cross-Origin Resource Sharing
5. **Authentication** - JWT token validation
6. **Authorization** - Role-based access control

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- SQL Server 2019 or later
- Visual Studio 2022 or VS Code

### Build and Run

```bash
# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run the API
dotnet run --project src/MealPreparationService.API

# The API will be available at:
# - HTTPS: https://127.0.0.1:5001
# - HTTP: http://127.0.0.1:5000
# - Swagger UI: https://127.0.0.1:5001/swagger
```

### Database Setup

```bash
# Add Entity Framework tools (if not already installed)
dotnet tool install --global dotnet-ef

# Create initial migration
dotnet ef migrations add InitialCreate --project src/MealPreparationService.DataAccess --startup-project src/MealPreparationService.API

# Update database
dotnet ef database update --project src/MealPreparationService.DataAccess --startup-project src/MealPreparationService.API
```

## API Endpoints

### Health Check
- `GET /api/health` - Check service health status

### Future Endpoints (to be implemented)
- Authentication & Authorization
- Meal Plan Management
- Virtual Fridge Management
- Grocery List Generation
- Order Management
- Delivery Tracking
- Nutrient Calculator

## Security Features

- **HTTPS Enforcement** - All traffic redirected to HTTPS
- **JWT Authentication** - Secure token-based authentication
- **Password Hashing** - BCrypt with work factor 12
- **Security Headers** - Protection against common web vulnerabilities
- **CORS Policy** - Restricted to configured origins
- **Input Validation** - Sanitization to prevent SQL injection and XSS

## Logging

Structured logging is configured with the following levels:
- **Debug** - Development environment only
- **Information** - General application flow
- **Warning** - Unexpected events that don't stop execution
- **Error** - Errors and exceptions
- **Critical** - Critical failures requiring immediate attention

## Next Steps

- Implement authentication and user management
- Add remaining domain entities
- Create business services for core features
- Implement API controllers
- Add data validation
- Create unit and integration tests
- Set up CI/CD pipeline

## License

Copyright © 2024 Meal Preparation Service. All rights reserved.
