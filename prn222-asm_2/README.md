# Meal Prep Service Application

A comprehensive ASP.NET Core MVC web application for meal preparation and delivery services, built with a clean 3-layer architecture.

## Project Structure

```
MealPrepService/
├── src/
│   ├── MealPrepService.Web/           # Presentation Layer (MVC)
│   │   ├── PresentationLayer/
│   │   │   ├── Controllers/           # MVC Controllers
│   │   │   ├── Views/                 # Views
│   │   │   ├── ViewModels/            # View Models
│   │   │   └── Filters/               # Authorization filters
│   │   ├── Data/                      # Database seeding
│   │   ├── wwwroot/                   # Static files
│   │   └── Program.cs                 # Application entry point
│   ├── MealPrepService.BusinessLogicLayer/  # Business Logic
│   │   ├── DTOs/                      # Data Transfer Objects
│   │   ├── Interfaces/                # Service interfaces
│   │   ├── Services/                  # Business logic services
│   │   └── Validators/                # FluentValidation validators
│   └── MealPrepService.DataAccessLayer/     # Data Access
│       ├── Data/                      # DbContext
│       ├── Entities/                  # Entity models
│       └── Repositories/              # Repository pattern
└── files/                             # Dataset Excel files
```

## Technology Stack

- **Framework**: ASP.NET Core 9.0 MVC
- **ORM**: Entity Framework Core 9.0
- **Database**: SQL Server (production), SQLite (development/testing)
- **Logging**: Serilog
- **Validation**: FluentValidation
- **Excel Processing**: EPPlus

## Features

- **User Management**: Account creation, authentication, role-based authorization
- **Health Profiles**: Dietary preferences, allergies, nutritional goals
- **Meal Planning**: AI-powered meal recommendations based on health profiles
- **Recipe Management**: 84 recipes with 240 ingredients
- **Order Management**: Meal ordering and delivery scheduling
- **Admin Dashboard**: Revenue reports, user management, dataset management

## Configuration

### Database

Configure in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=MealPrepService;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "UseSqlite": false
}
```

Set `UseSqlite` to `true` for SQLite or `false` for SQL Server.

### Dataset Import

The application automatically imports data from Excel files in the `files/` directory:
- 20 allergies
- 240 ingredients
- 84 recipes

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- SQL Server (or use SQLite for development)

### Setup

1. **Clone the repository**

2. **Configure database** (see [SETUP_AND_TROUBLESHOOTING.md](SETUP_AND_TROUBLESHOOTING.md))

3. **Run migrations**:
   ```bash
   cd src/MealPrepService.DataAccessLayer
   dotnet ef database update
   ```

4. **Run the application**:
   ```bash
   cd src/MealPrepService.Web
   dotnet run
   ```
   Or press F5 in Visual Studio

5. **Login with admin account**:
   - Email: `admin@mealprep.com`
   - Password: `Admin@123`

### Building and Testing

```bash
# Build
dotnet build

# Run all tests
dotnet test

# Run specific test category
dotnet test --filter "Feature=dataset-import-service"
```

## Architecture Principles

- **Clean Architecture**: Strict separation of concerns across three layers
- **Dependency Injection**: All cross-layer dependencies managed through DI
- **Repository Pattern**: Data access abstraction
- **Unit of Work Pattern**: Transaction management
- **DTO/ViewModel Pattern**: Data transfer between layers
- **SOLID Principles**: Single responsibility, dependency inversion

## License

Copyright © 2026 Meal Prep Service
