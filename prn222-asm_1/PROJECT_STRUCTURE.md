# Meal Prep Service - Project Structure Documentation

This document provides a comprehensive overview of the project structure, explaining the purpose and function of each folder and its contents.

---

## 📁 Root Level Structure

### `/src`
Contains all source code for the application, organized into three main layers following Clean Architecture principles.

### `/files`
Contains dataset files used for seeding the database:
- `Dataset_Allergy.xlsx` - Allergy data
- `Dataset_Ingredient.xlsx` - Ingredient data
- `Dataset_Recipe.xlsx` - Recipe data
- `Dataset_Recipe_Ingredient.xlsx` - Recipe-Ingredient relationships

### `/diagrams`
Contains system design diagrams:
- `context_diagram.drawio.png` - System context diagram
- `erd.txt` - Entity Relationship Diagram
- `use_case_diagram.drawio.png` - Use case diagram
- `workflows_diagram_swimlane.drawio.png` - Workflow swimlane diagram

---

## 🏗️ Layer 1: Data Access Layer (DAL)
**Path:** `src/MealPrepService.DataAccessLayer`

### Purpose
Handles all database operations, entity definitions, and data persistence logic. Implements the Repository and Unit of Work patterns.

### 📂 `/Data`
**Purpose:** Database context and configuration

**Files:**
- `MealPrepDbContext.cs` - Entity Framework Core DbContext that manages database connections, entity configurations, relationships, indexes, and constraints

**Key Responsibilities:**
- Configure entity relationships (one-to-one, one-to-many, many-to-many)
- Define database indexes for performance optimization
- Set up constraints and validation rules
- Manage database migrations

---

### 📂 `/Entities`
**Purpose:** Domain models that represent database tables

**Key Files:**
- `Account.cs` - User accounts (Admin, Manager, Customer, DeliveryMan)
- `HealthProfile.cs` - Customer health information and dietary preferences
- `Allergy.cs` - Allergy definitions
- `Ingredient.cs` - Food ingredients with nutritional information
- `Recipe.cs` - Meal recipes with instructions and nutrition
- `RecipeIngredient.cs` - Junction table linking recipes to ingredients
- `MealPlan.cs` - Customer meal plans
- `Meal.cs` - Individual meals within a plan
- `MealRecipe.cs` - Junction table linking meals to recipes
- `DailyMenu.cs` - Daily menus created by managers
- `MenuMeal.cs` - Meals available in daily menus
- `FridgeItem.cs` - Customer's virtual fridge inventory
- `Order.cs` - Customer orders (Checkpoint 2)
- `OrderDetail.cs` - Order line items (Checkpoint 2)
- `DeliverySchedule.cs` - Delivery information (Checkpoint 2)
- `RevenueReport.cs` - Monthly revenue reports
- `SubscriptionPackage.cs` - Subscription plans (Checkpoint 2)
- `UserSubscription.cs` - Customer subscriptions (Checkpoint 2)
- `AIConfiguration.cs` - AI service configuration
- `AIOperationLog.cs` - AI operation logging
- `SystemConfiguration.cs` - System-wide configuration settings
- `BaseEntity.cs` - Base class with common properties (Id, CreatedAt, UpdatedAt)

**Key Responsibilities:**
- Define database schema through code
- Establish entity relationships
- Provide navigation properties for related data

---

### 📂 `/Repositories`
**Purpose:** Implement data access patterns for database operations

**Interface Files:**
- `IRepository.cs` - Generic repository interface for CRUD operations
- `IUnitOfWork.cs` - Unit of Work pattern interface for transaction management
- `IAccountRepository.cs` - Account-specific operations
- `IMealPlanRepository.cs` - Meal plan-specific operations
- `IRecipeRepository.cs` - Recipe-specific operations
- `IOrderRepository.cs` - Order-specific operations
- `IDailyMenuRepository.cs` - Menu-specific operations
- `IFridgeItemRepository.cs` - Fridge item-specific operations
- `IUserSubscriptionRepository.cs` - Subscription-specific operations

**Implementation Files:**
- `Repository.cs` - Generic repository implementation
- `UnitOfWork.cs` - Coordinates multiple repositories in a single transaction
- `AccountRepository.cs` - Account data access with email lookup
- `MealPlanRepository.cs` - Meal plan data access with related meals and recipes
- `RecipeRepository.cs` - Recipe data access with ingredients
- `OrderRepository.cs` - Order data access with details
- `DailyMenuRepository.cs` - Menu data access by date and week
- `FridgeItemRepository.cs` - Fridge item data access with expiry tracking
- `UserSubscriptionRepository.cs` - Subscription data access
- `RecipeIngredientExtensions.cs` - Extension methods for recipe ingredients

**Key Responsibilities:**
- Abstract database operations from business logic
- Provide reusable data access methods
- Manage database transactions
- Handle complex queries with related data

---

### 📂 `/Migrations`
**Purpose:** Entity Framework Core database migrations

**Key Files:**
- `*_InitialCreate.cs` - Initial database schema
- `*_AddIsActiveToMealPlan.cs` - Add IsActive flag to meal plans
- `*_AddFoodPreferencesTextField.cs` - Add food preferences field
- `*_AddMealFinishedToMeal.cs` - Add meal completion tracking
- `*_AddIngredientAllergyRelationship.cs` - Link ingredients to allergies
- `*_AddSystemConfiguration.cs` - Add system configuration table
- `MealPrepDbContextModelSnapshot.cs` - Current database schema snapshot

**Key Responsibilities:**
- Version control for database schema
- Enable database updates and rollbacks
- Track schema changes over time

---

## 🧠 Layer 2: Business Logic Layer (BLL)
**Path:** `src/MealPrepService.BusinessLogicLayer`

### Purpose
Contains all business rules, validation logic, and application services. Acts as the intermediary between the presentation layer and data access layer.

### 📂 `/DTOs` (Data Transfer Objects)
**Purpose:** Define data structures for transferring data between layers

**Key Files:**
- `AccountDto.cs` - Account data transfer
- `CreateAccountDto.cs` - Account creation data
- `UpdateAccountDto.cs` - Account update data
- `LoginDto.cs` - Login credentials
- `HealthProfileDto.cs` - Health profile data
- `AllergyDto.cs` - Allergy data
- `IngredientDto.cs` - Ingredient data with nutrition
- `CreateIngredientDto.cs` - Ingredient creation data
- `UpdateIngredientDto.cs` - Ingredient update data
- `RecipeDto.cs` - Recipe data with ingredients
- `CreateRecipeDto.cs` - Recipe creation data
- `UpdateRecipeDto.cs` - Recipe update data
- `RecipeIngredientDto.cs` - Recipe-ingredient relationship
- `MealPlanDto.cs` - Meal plan data
- `MealDto.cs` - Meal data
- `DailyMenuDto.cs` - Daily menu data
- `MenuMealDto.cs` - Menu meal data
- `FridgeItemDto.cs` - Fridge item data
- `GroceryListDto.cs` - Generated grocery list
- `OrderDto.cs` - Order data (Checkpoint 2)
- `OrderDetailDto.cs` - Order detail data (Checkpoint 2)
- `OrderItemDto.cs` - Order item data (Checkpoint 2)
- `DeliveryScheduleDto.cs` - Delivery data (Checkpoint 2)
- `RevenueReportDto.cs` - Revenue report data
- `DashboardStatsDto.cs` - Dashboard statistics
- `MealRecommendation.cs` - AI meal recommendations
- `CustomerContext.cs` - Customer profile context for AI
- `VnpayPaymentUrlDto.cs` - Payment URL data (Checkpoint 2)
- `VnpayCallbackDto.cs` - Payment callback data (Checkpoint 2)
- `VnpayCallbackResult.cs` - Payment result (Checkpoint 2)
- `SystemConfigurationDto.cs` - System configuration data

**Key Responsibilities:**
- Decouple entities from presentation layer
- Provide clean data contracts
- Enable data validation at boundaries
- Prevent over-posting vulnerabilities

---

### 📂 `/Interfaces`
**Purpose:** Define contracts for business services

**Key Files:**
- `IAccountService.cs` - Account management operations
- `IPasswordHasher.cs` - Password hashing operations
- `IHealthProfileService.cs` - Health profile management
- `IAllergyService.cs` - Allergy management
- `IIngredientService.cs` - Ingredient management
- `IRecipeService.cs` - Recipe management
- `IMealPlanService.cs` - Meal plan management
- `IMenuService.cs` - Daily menu management
- `IFridgeService.cs` - Fridge inventory management
- `IOrderService.cs` - Order management (Checkpoint 2)
- `IDeliveryService.cs` - Delivery management (Checkpoint 2)
- `IRevenueService.cs` - Revenue reporting
- `IVnpayService.cs` - Payment processing (Checkpoint 2)
- `IAIRecommendationService.cs` - AI meal recommendations
- `IRecommendationEngine.cs` - Recommendation engine
- `ILLMService.cs` - Large Language Model service
- `ICustomerProfileAnalyzer.cs` - Customer profile analysis
- `IAIConfigurationService.cs` - AI configuration management
- `IAIOperationLogger.cs` - AI operation logging
- `ISystemConfigurationService.cs` - System configuration management

**Key Responsibilities:**
- Define service contracts
- Enable dependency injection
- Support unit testing through mocking
- Promote loose coupling

---

### 📂 `/Services`
**Purpose:** Implement business logic and orchestrate operations

**Key Files:**
- `AccountService.cs` - User authentication, registration, staff management
- `PasswordHasher.cs` - BCrypt password hashing
- `HealthProfileService.cs` - Health profile CRUD, allergy management
- `AllergyService.cs` - Allergy CRUD operations
- `IngredientService.cs` - Ingredient CRUD with pagination and search
- `RecipeService.cs` - Recipe CRUD with ingredient management
- `MealPlanService.cs` - Manual and AI-generated meal plans
- `MenuService.cs` - Daily menu creation, publishing, activation/deactivation
- `FridgeService.cs` - Fridge inventory, grocery list generation
- `OrderService.cs` - Order processing (Checkpoint 2)
- `DeliveryService.cs` - Delivery scheduling (Checkpoint 2)
- `RevenueService.cs` - Revenue calculation and reporting
- `VnpayService.cs` - VNPay payment integration (Checkpoint 2)
- `AIRecommendationService.cs` - AI-powered meal recommendations
- `AIRecommendationEngine.cs` - Recommendation algorithm
- `OpenAIRecommendationService.cs` - OpenAI GPT integration
- `CustomerProfileAnalyzer.cs` - Customer preference analysis
- `AIConfigurationService.cs` - AI configuration management
- `AIOperationLogger.cs` - AI operation logging
- `SystemConfigurationService.cs` - System settings management

**Key Responsibilities:**
- Implement business rules and validation
- Coordinate multiple repository operations
- Handle complex business workflows
- Enforce authorization and security rules
- Integrate with external services (AI, payment)

---

### 📂 `/Exceptions`
**Purpose:** Define custom exception types for business logic errors

**Key Files:**
- `BusinessException.cs` - Base exception for business rule violations
- `NotFoundException.cs` - Resource not found errors
- `ValidationException.cs` - Data validation errors
- `AuthenticationException.cs` - Authentication failures
- `AuthorizationException.cs` - Authorization failures
- `ConstraintViolationException.cs` - Database constraint violations

**Key Responsibilities:**
- Provide meaningful error messages
- Enable specific error handling
- Separate business errors from system errors
- Support proper HTTP status code mapping

---

### 📂 `/Validators`
**Purpose:** FluentValidation validators for DTOs

**Key Files:**
- `CreateAccountDtoValidator.cs` - Account creation validation
- `UpdateAccountDtoValidator.cs` - Account update validation
- `LoginDtoValidator.cs` - Login validation
- `HealthProfileDtoValidator.cs` - Health profile validation
- `CreateIngredientDtoValidator.cs` - Ingredient creation validation
- `UpdateIngredientDtoValidator.cs` - Ingredient update validation
- `CreateRecipeDtoValidator.cs` - Recipe creation validation
- `UpdateRecipeDtoValidator.cs` - Recipe update validation
- `MealPlanDtoValidator.cs` - Meal plan validation
- `MenuMealDtoValidator.cs` - Menu meal validation
- `FridgeItemDtoValidator.cs` - Fridge item validation
- `OrderDtoValidator.cs` - Order validation (Checkpoint 2)
- `OrderDetailDtoValidator.cs` - Order detail validation (Checkpoint 2)
- `DeliveryScheduleDtoValidator.cs` - Delivery validation (Checkpoint 2)

**Key Responsibilities:**
- Validate input data before processing
- Provide detailed validation error messages
- Enforce business rules at the boundary
- Support client-side validation

---

## 🎨 Layer 3: Presentation Layer (Web)
**Path:** `src/MealPrepService.Web`

### Purpose
ASP.NET Core MVC web application that provides the user interface and handles HTTP requests.

### 📂 `/PresentationLayer/Controllers`
**Purpose:** Handle HTTP requests and coordinate between views and services

**Key Files:**
- `HomeController.cs` - Home page and general navigation
- `AccountController.cs` - Login, registration, logout
- `HealthProfileController.cs` - Health profile management
- `MealPlanController.cs` - Meal plan creation and management
- `FridgeController.cs` - Fridge inventory and grocery lists
- `RecipeController.cs` - Recipe CRUD (Manager/Admin)
- `IngredientController.cs` - Ingredient CRUD (Manager/Admin)
- `AllergyController.cs` - Allergy CRUD (Manager/Admin)
- `MenuController.cs` - Daily menu management (Manager/Admin)
- `PublicMenuController.cs` - Public menu viewing (all users)
- `OrderController.cs` - Order placement (Checkpoint 2)
- `DeliveryController.cs` - Delivery management (Checkpoint 2)
- `AdminController.cs` - Admin dashboard, staff management, revenue reports, system configuration
- `AITestController.cs` - AI testing interface (development)

**Key Responsibilities:**
- Route HTTP requests to appropriate actions
- Validate user input
- Call business services
- Map DTOs to ViewModels
- Return views or redirect responses
- Handle authentication and authorization

---

### 📂 `/PresentationLayer/Views`
**Purpose:** Razor views for rendering HTML

**Subfolders:**
- `/Account` - Login, registration, access denied pages
- `/HealthProfile` - Health profile views (index, create, edit)
- `/MealPlan` - Meal plan views (index, create, details, add meal)
- `/Fridge` - Fridge views (index, add item, grocery list)
- `/Recipe` - Recipe views (index, create, edit, details)
- `/Ingredient` - Ingredient views (index, create, edit, details)
- `/Allergy` - Allergy views (index, create, edit)
- `/Menu` - Menu management views (index, create, details, add meal)
- `/PublicMenu` - Public menu views (today, weekly)
- `/Order` - Order views (Checkpoint 2)
- `/Delivery` - Delivery views (Checkpoint 2)
- `/Admin` - Admin views (dashboard, staff accounts, revenue, system configuration)
- `/AITest` - AI testing views (development)
- `/Home` - Home page and privacy policy
- `/Shared` - Shared layouts, partials, error pages

**Key Files in /Shared:**
- `_Layout.cshtml` - Main layout template with navigation
- `_LoginPartial.cshtml` - Login/logout partial view
- `Error.cshtml` - Error page
- `_ValidationScriptsPartial.cshtml` - Client-side validation scripts
- `_OrderStatusBadge.cshtml` - Order status badge partial (Checkpoint 2)
- `_OrderProgressIndicator.cshtml` - Order progress partial (Checkpoint 2)
- `_PaymentMethodIcon.cshtml` - Payment method icon partial (Checkpoint 2)
- `_PaymentHelpText.cshtml` - Payment help text partial (Checkpoint 2)

**Key Responsibilities:**
- Render HTML using Razor syntax
- Display data from ViewModels
- Provide user interface components
- Handle client-side interactions
- Support responsive design

---

### 📂 `/PresentationLayer/ViewModels`
**Purpose:** Define data structures specifically for views

**Key Files:**
- `LoginViewModel.cs` - Login form data
- `RegisterViewModel.cs` - Registration form data
- `HealthProfileViewModel.cs` - Health profile display and editing
- `AllergyViewModel.cs` - Allergy display and selection
- `MealPlanViewModel.cs` - Meal plan display
- `FridgeViewModel.cs` - Fridge inventory display
- `RecipeViewModel.cs` - Recipe display and editing
- `IngredientViewModel.cs` - Ingredient display and editing
- `MenuViewModel.cs` - Menu display and management
- `OrderViewModel.cs` - Order display (Checkpoint 2)
- `DeliveryViewModel.cs` - Delivery display (Checkpoint 2)
- `AdminViewModel.cs` - Admin dashboard, staff accounts, revenue reports
- `SystemConfigurationViewModel.cs` - System configuration editing
- `ErrorViewModel.cs` - Error page data

**Key Responsibilities:**
- Adapt DTOs for view-specific needs
- Include display-only properties
- Support data annotations for validation
- Provide computed properties for UI logic

---

### 📂 `/PresentationLayer/Filters`
**Purpose:** Custom action filters and authorization handlers

**Key Files:**
- `AuthorizeRolesAttribute.cs` - Role-based authorization attribute
- `RoleAuthorizationAttribute.cs` - Custom role authorization
- `ResourceOwnerAuthorizationHandler.cs` - Resource ownership authorization

**Key Responsibilities:**
- Enforce role-based access control
- Validate resource ownership
- Implement custom authorization policies
- Handle authorization failures

---

### 📂 `/PresentationLayer/Middleware`
**Purpose:** Custom middleware components

**Key Files:**
- `GlobalExceptionHandler.cs` - Global exception handling
- `GlobalExceptionHandlerExtensions.cs` - Extension methods for middleware

**Key Responsibilities:**
- Catch and handle unhandled exceptions
- Log errors
- Return appropriate error responses
- Prevent sensitive information leakage

---

### 📂 `/Data`
**Purpose:** Database seeding and data import

**Key Files:**
- `DbSeeder.cs` - Seeds initial data (admin account, system configuration)
- `DatasetImporter.cs` - Imports data from Excel files

**Key Responsibilities:**
- Initialize database with default data
- Import datasets from Excel files
- Ensure database is ready for use

---

### 📂 `/wwwroot`
**Purpose:** Static files (CSS, JavaScript, images)

**Subfolders:**
- `/css` - Stylesheets (site.css)
- `/js` - JavaScript files (site.js)
- `/lib` - Third-party libraries (Bootstrap, jQuery, jQuery Validation)

**Key Responsibilities:**
- Serve static assets
- Provide client-side functionality
- Style the application

---

### 📂 Root Configuration Files

**Key Files:**
- `Program.cs` - Application entry point, service registration, middleware configuration
- `appsettings.json` - Application configuration (connection strings, logging, AI settings)
- `appsettings.Development.json` - Development-specific configuration

**Key Responsibilities:**
- Configure dependency injection
- Set up middleware pipeline
- Configure authentication and authorization
- Register services and repositories
- Configure database connection
- Set up logging

---

## 🔄 Data Flow

### Typical Request Flow:
1. **User Request** → Browser sends HTTP request
2. **Routing** → ASP.NET Core routes to appropriate Controller action
3. **Authentication/Authorization** → Middleware validates user identity and permissions
4. **Controller** → Receives request, validates input
5. **Service** → Controller calls business service
6. **Validation** → FluentValidation validates DTOs
7. **Repository** → Service calls repository for data access
8. **Database** → Repository queries/updates database via Entity Framework
9. **Mapping** → Data mapped through DTOs and ViewModels
10. **View** → Controller returns view with ViewModel
11. **Response** → HTML rendered and sent to browser

---

## 🔐 Security Features

### Authentication
- Cookie-based authentication
- Password hashing with BCrypt
- Session management

### Authorization
- Role-based access control (Admin, Manager, Customer, DeliveryMan)
- Resource ownership validation
- Custom authorization policies

### Data Protection
- Anti-forgery tokens on forms
- Input validation with FluentValidation
- SQL injection prevention via Entity Framework
- XSS prevention via Razor encoding

---

## 🤖 AI Integration

### Components
- **OpenAI GPT Integration** - Generates meal recommendations
- **Customer Profile Analyzer** - Analyzes customer preferences and health data
- **Recommendation Engine** - Matches recipes to customer profiles
- **Operation Logger** - Tracks AI operations for monitoring

### Features
- AI-generated meal plans based on health profiles
- Personalized recipe recommendations
- Dietary restriction compliance
- Calorie goal optimization

---

## 📊 Key Features by Role

### Customer
- Health profile management with allergies
- AI-generated and manual meal plans
- Virtual fridge inventory
- Grocery list generation
- Public menu viewing
- Order placement (Checkpoint 2)

### Manager
- Recipe management
- Ingredient management
- Allergy management
- Daily menu creation and publishing
- Menu activation/deactivation

### Admin
- All Manager features
- Staff account management (Manager, DeliveryMan)
- Revenue reports and dashboard
- System configuration (meal plan limits, fridge limits)

### DeliveryMan
- View assigned deliveries (Checkpoint 2)
- Update delivery status (Checkpoint 2)

---

## 🗄️ Database Schema

### Core Tables
- Accounts, HealthProfiles, Allergies
- Ingredients, Recipes, RecipeIngredients
- MealPlans, Meals, MealRecipes
- DailyMenus, MenuMeals
- FridgeItems
- Orders, OrderDetails, DeliverySchedules (Checkpoint 2)
- RevenueReports
- SubscriptionPackages, UserSubscriptions (Checkpoint 2)
- AIConfigurations, AIOperationLogs
- SystemConfigurations

### Junction Tables
- HealthProfileAllergies (Many-to-Many)
- IngredientAllergies (Many-to-Many)
- RecipeIngredients (Many-to-Many with additional data)
- MealRecipes (Many-to-Many)

---

## 🚀 Deployment Considerations

### Configuration
- Connection strings in appsettings.json
- Environment-specific settings
- AI API keys (OpenAI)
- Payment gateway credentials (VNPay) (Checkpoint 2)

### Database
- SQLite for development
- SQL Server for production
- Automatic migrations on startup

### Logging
- Serilog for structured logging
- File-based logs in /logs folder
- Console logging for development

---

## 📝 Notes

### Checkpoint 1 (Current)
- Core features implemented
- Order and delivery features disabled (UI shows "Coming Soon")
- Subscription features deferred

### Checkpoint 2 (Future)
- Order placement and management
- Delivery scheduling and tracking
- Payment integration (VNPay)
- Subscription packages

---

## 🛠️ Technology Stack

- **Framework:** ASP.NET Core 9.0 MVC
- **ORM:** Entity Framework Core
- **Database:** SQLite (dev), SQL Server (prod)
- **Validation:** FluentValidation
- **Authentication:** ASP.NET Core Identity (Cookie-based)
- **Logging:** Serilog
- **AI:** OpenAI GPT API
- **Frontend:** Bootstrap 5, jQuery
- **Password Hashing:** BCrypt.Net

---

*Last Updated: January 25, 2026*
*Version: Checkpoint 1*
