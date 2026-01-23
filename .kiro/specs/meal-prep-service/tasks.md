# Implementation Plan: Meal Prep Service Application

## Overview

This implementation plan breaks down the Meal Prep Service Application into discrete coding tasks following the 3-layer architecture (Data Access, Business Logic, Presentation). The plan emphasizes incremental development with early validation through property-based testing and unit tests. Each task builds on previous work to ensure a cohesive, working system.

## Tasks

- [x] 1. Set up project structure and core infrastructure
  - Create ASP.NET Core MVC project with 3-layer architecture
  - Configure Entity Framework Core with SQL Server and SQLite providers
  - Set up dependency injection container
  - Configure Serilog logging
  - Install required NuGet packages (EF Core, FluentValidation, FsCheck.Xunit, Moq)
  - Create base entity classes and common interfaces
  - _Requirements: 16.1, 16.2, 16.3, 16.8_

- [x] 2. Implement Data Access Layer - Core entities and DbContext
  - [x] 2.1 Create entity models for Account, HealthProfile, Allergy, FoodPreference
    - Implement Account entity with navigation properties
    - Implement HealthProfile entity with relationships
    - Implement Allergy and FoodPreference entities
    - _Requirements: 1.1, 2.1_
  
  - [x] 2.2 Create entity models for Subscription and MealPlan
    - Implement SubscriptionPackage and UserSubscription entities
    - Implement MealPlan, Meal, Recipe, Ingredient entities
    - Implement RecipeIngredient join entity
    - _Requirements: 3.1, 4.1, 5.1, 11.1, 11.3_
  
  - [x] 2.3 Create entity models for Orders and Delivery
    - Implement DailyMenu, MenuMeal entities
    - Implement Order, OrderDetail entities
    - Implement DeliverySchedule entity
    - Implement FridgeItem and RevenueReport entities
    - _Requirements: 7.1, 8.1, 9.1, 10.1, 12.1_
  
  - [x] 2.4 Configure DbContext with relationships and constraints
    - Implement MealPrepDbContext with all DbSets
    - Configure one-to-one, one-to-many, many-to-many relationships
    - Configure indexes for performance
    - Configure check constraints for data validation
    - Add database migrations
    - _Requirements: 16.1_


- [x] 3. Implement Repository Pattern and Unit of Work
  - [x] 3.1 Create generic repository interface and implementation
    - Implement IRepository<T> interface
    - Implement Repository<T> base class with CRUD operations
    - _Requirements: 16.1_
  
  - [x] 3.2 Create specialized repository interfaces
    - Implement IAccountRepository with email lookup methods
    - Implement IUserSubscriptionRepository with active subscription queries
    - Implement IMealPlanRepository with account filtering
    - Implement IRecipeRepository with ingredient and allergen filtering
    - Implement IOrderRepository with date range queries
    - Implement IDailyMenuRepository with date-based queries
    - Implement IFridgeItemRepository with expiry queries
    - _Requirements: 16.1_
  
  - [x] 3.3 Implement specialized repositories
    - Implement AccountRepository
    - Implement UserSubscriptionRepository
    - Implement MealPlanRepository
    - Implement RecipeRepository
    - Implement OrderRepository
    - Implement DailyMenuRepository
    - Implement FridgeItemRepository
    - _Requirements: 16.1_
  
  - [x] 3.4 Implement Unit of Work pattern
    - Create IUnitOfWork interface
    - Implement UnitOfWork class with transaction support
    - Register repositories in UnitOfWork
    - _Requirements: 16.7_
  
  - [x] 3.5 Write property tests for repository operations


    - **Property 19: Meal plan account association**
    - **Property 30: Fridge item deletion**
    - **Property 54: Allergen flag storage**
    - **Validates: Requirements 4.6, 7.3, 11.4**

- [x] 4. Implement Business Logic Layer - Account and Authentication Services
  - [x] 4.1 Create DTOs for Account and HealthProfile
    - Implement AccountDto, CreateAccountDto, LoginDto
    - Implement HealthProfileDto with allergy and preference lists
    - _Requirements: 16.6_
  
  - [x] 4.2 Implement password hashing service
    - Create IPasswordHasher interface
    - Implement PasswordHasher using BCrypt or PBKDF2
    - _Requirements: 1.6_
  
  - [x] 4.3 Implement AccountService
    - Implement RegisterAsync with email validation and password hashing
    - Implement AuthenticateAsync with credential verification
    - Implement GetByIdAsync and EmailExistsAsync
    - _Requirements: 1.1, 1.2, 1.4, 1.5, 1.6_
  
  - [x] 4.4 Write property tests for AccountService

    - **Property 1: Account creation sets customer role**
    - **Property 2: Duplicate email rejection**
    - **Property 3: Authentication with valid credentials**
    - **Property 4: Authentication with invalid credentials**
    - **Property 5: Password hashing**
    - **Validates: Requirements 1.1, 1.2, 1.4, 1.5, 1.6**
  
  - [x] 4.5 Implement HealthProfileService
    - Implement CreateOrUpdateAsync with validation
    - Implement AddAllergyAsync and RemoveAllergyAsync
    - Implement AddFoodPreferenceAsync and RemoveFoodPreferenceAsync
    - Implement GetByAccountIdAsync
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_
  
  - [x] 4.6 Write property tests for HealthProfileService

    - **Property 7: Health profile storage**
    - **Property 8: Allergy and preference link management**
    - **Property 9: Positive weight and height validation**
    - **Property 10: Age range validation**
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7**


- [ ] 5. DEFERRED TO CHECKPOINT 2: Implement Business Logic Layer - Subscription Services
  - [ ] 5.1 Create DTOs for Subscription
    - Implement SubscriptionPackageDto
    - Implement UserSubscriptionDto
    - _Requirements: 16.6_
  
  - [ ] 5.2 Implement SubscriptionService
    - Implement GetAllPackagesAsync
    - Implement SubscribeAsync with date calculation
    - Implement GetActiveSubscriptionAsync
    - Implement HasActiveSubscriptionAsync
    - Implement UpdateExpiredSubscriptionsAsync
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_
  
  - [ ]* 5.3 Write property tests for SubscriptionService
    - **Property 11: Subscription package display**
    - **Property 12: Subscription date calculation**
    - **Property 13: Subscription status lifecycle**
    - **Property 14: Subscription requirement enforcement**
    - **Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5**

- [ ] 6. DEFERRED TO CHECKPOINT 2: Implement Standalone Nutrition Calculator Service
  - [ ] 6.1 Create NutritionInfo DTO
    - Implement NutritionInfo with calories and macros
    - _Requirements: 16.6_
  
  - [ ] 6.2 Implement NutritionCalculatorService (standalone feature)
    - Implement CalculateFromIngredientsAsync - user inputs ingredients, get nutrition
    - This is a STANDALONE feature for users to calculate nutrition manually
    - _Requirements: 6.1, 6.2_
  
  - [ ]* 6.3 Write property tests for standalone NutritionCalculatorService
    - **Property 24: Recipe nutrition calculation from user input**
    - **Validates: Requirements 6.1, 6.2**

- [x] 7. Implement Business Logic Layer - Meal Plan Services (WITH nutrition display from existing data)
  - [x] 7.1 Create DTOs for MealPlan
    - Implement MealPlanDto, MealDto, RecipeDto
    - Include nutrition fields in RecipeDto (TotalCalories, ProteinG, FatG, CarbsG)
    - _Requirements: 16.6_
  
  - [x] 7.2 Implement MealPlanService
    - Implement CreateManualMealPlanAsync with date validation
    - Implement AddMealToPlanAsync with date range validation
    - Implement GetByIdAsync and GetByAccountIdAsync
    - Include nutrition data from recipes when returning meal plans
    - NOTE: Skip subscription checks for now (deferred to checkpoint 2)
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.6, 6.3, 6.4_
  
  - [x] 7.3 Implement AI Meal Plan Generator (stub for now)
    - Implement GenerateAiMealPlanAsync with health profile retrieval
    - Implement allergen exclusion logic
    - Implement food preference prioritization
    - Set is_ai_generated flag correctly
    - AI should populate nutrition data in recipes when generating plans
    - NOTE: Skip subscription checks for now (deferred to checkpoint 2)
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_
  
  - [x] 7.4 Write property tests for MealPlanService

    - **Property 15: Meal plan date range coverage**
    - **Property 16: Allergen exclusion**
    - **Property 17: Food preference prioritization**
    - **Property 18: AI generation flag correctness**
    - **Property 20: Manual meal plan creation**
    - **Property 21: Meal date range validation**
    - **Property 22: Recipe-meal linking**
    - **Property 23: Personal recipe creation**
    - **Property 25: Meal nutrition aggregation** (from existing recipe data)
    - **Property 26: Daily nutrition aggregation** (from existing recipe data)
    - **Validates: Requirements 4.2, 4.3, 4.4, 4.5, 5.1, 5.2, 5.3, 5.4, 5.6, 6.3, 6.4**

- [ ] 8. Checkpoint 1 - Ensure all tests pass (excluding subscription and standalone nutrition calculator)
  - Run all unit tests and property tests
  - Verify database migrations work correctly
  - Ensure all services are properly registered in DI container
  - NOTE: Subscription and nutrition calculator features deferred to checkpoint 2
  - Ask the user if questions arise


- [x] 9. Implement Business Logic Layer - Fridge and Menu Services (WITHOUT subscription checks)
  - [x] 9.1 Create DTOs for Fridge and Menu
    - Implement FridgeItemDto, GroceryListDto
    - Implement DailyMenuDto, MenuMealDto
    - _Requirements: 16.6_
  
  - [x] 9.2 Implement FridgeService
    - Implement GetFridgeItemsAsync
    - Implement AddItemAsync with validation
    - Implement UpdateItemQuantityAsync with non-negative validation
    - Implement RemoveItemAsync
    - Implement GetExpiringItemsAsync with date comparison
    - Implement GenerateGroceryListAsync with inventory comparison
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7_
  
  - [x] 9.3 Write property tests for FridgeService

    - **Property 28: Fridge item storage**
    - **Property 29: Non-negative quantity validation**
    - **Property 31: Fridge item retrieval**
    - **Property 32: Grocery list generation**
    - **Property 33: Expiry status determination**
    - **Validates: Requirements 7.1, 7.2, 7.4, 7.5, 7.6, 7.7**
  
  - [x] 9.4 Implement MenuService
    - Implement CreateDailyMenuAsync with draft status
    - Implement AddMealToMenuAsync with required field validation
    - Implement PublishMenuAsync with status transition
    - Implement UpdateMealQuantityAsync with non-negative validation
    - Implement GetByDateAsync and GetWeeklyMenuAsync
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7_
  
  - [x] 9.5 Write property tests for MenuService

    - **Property 34: Menu creation with draft status**
    - **Property 35: Menu meal required fields**
    - **Property 36: Menu status transitions**
    - **Property 37: Menu quantity validation**
    - **Property 38: Weekly menu date range**
    - **Property 39: Sold out status**
    - **Validates: Requirements 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7**

- [x] 10. Implement Business Logic Layer - Order and Delivery Services (WITHOUT subscription checks)
  - [x] 10.1 Create DTOs for Order and Delivery
    - Implement OrderDto, OrderDetailDto, OrderItemDto
    - Implement DeliveryScheduleDto
    - _Requirements: 16.6_
  
  - [x] 10.2 Implement OrderService
    - Implement CreateOrderAsync with quantity validation and inventory reduction
    - Implement ProcessPaymentAsync with transaction management
    - Implement payment failure rollback logic
    - Implement GetByIdAsync and GetByAccountIdAsync
    - Implement UpdateOrderStatusAsync
    - NOTE: Skip subscription checks for now (deferred to checkpoint 2)
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7_
  
  - [x] 10.3 Write property tests for OrderService

    - **Property 40: Order detail creation**
    - **Property 41: Order quantity validation**
    - **Property 42: Order total calculation**
    - **Property 43: Inventory reduction on order**
    - **Property 44: Payment failure rollback**
    - **Property 45: Payment success workflow**
    - **Validates: Requirements 9.1, 9.2, 9.3, 9.4, 9.6, 9.7**
  
  - [x] 10.4 Implement DeliveryService
    - Implement CreateDeliveryScheduleAsync
    - Implement GetByAccountIdAsync and GetByDeliveryManAsync
    - Implement CompleteDeliveryAsync with order status update
    - Implement UpdateDeliveryTimeAsync with future date validation
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_
  
  - [x] 10.5 Write property tests for DeliveryService

    - **Property 46: Delivery schedule creation**
    - **Property 47: Customer delivery retrieval**
    - **Property 48: Delivery man assignment filtering**
    - **Property 49: Delivery completion status update**
    - **Property 50: Future delivery time validation**
    - **Validates: Requirements 10.1, 10.2, 10.3, 10.4, 10.5**

- [x] 10.6 ENHANCEMENT: Implement Enhanced Payment System (COD and VNPAY)
  - [x] 10.6.1 Update Order entity and DTOs for enhanced payment
    - Add VnpayTransactionId, PaymentConfirmedAt, PaymentConfirmedBy fields to Order entity
    - Update OrderDto with new payment-related fields
    - Create VnpayPaymentUrlDto, VnpayCallbackDto, VnpayCallbackResult DTOs
    - Update database migration for new Order fields
    - _Requirements: 9.5, 9.6, 9.7, 9.10_
  
  - [x] 10.6.2 Implement VnpayService for VNPAY integration
    - Implement CreatePaymentUrlAsync with secure hash generation
    - Implement ProcessCallbackAsync with signature validation
    - Implement ValidateCallback with HMAC-SHA512 verification
    - Configure VNPAY settings (URL, TmnCode, HashSecret, ReturnUrl)
    - Handle all VNPAY response codes and error messages
    - _Requirements: 9.6, 9.7_
  
  - [x] 10.6.3 Enhance OrderService for multiple payment methods
    - Update ProcessPaymentAsync to handle COD and VNPAY methods
    - Implement ProcessVnpayCallbackAsync for payment confirmations
    - Implement ConfirmCashPaymentAsync for delivery man cash confirmations
    - Update order status workflow (pending -> pending_payment -> confirmed)
    - Ensure delivery schedule creation for both payment methods
    - _Requirements: 9.5, 9.6, 9.7, 9.10, 9.11, 9.12_
  
  - [x] 10.6.4 Write property tests for enhanced payment system

    - **Property 69: COD order status workflow**
    - **Property 70: VNPAY payment callback processing**
    - **Property 71: Cash payment confirmation authorization**
    - **Property 72: Payment method validation**
    - **Property 73: VNPAY callback validation**
    - **Validates: Requirements 9.5, 9.6, 9.7, 9.10, 10.6, 10.7**


- [x] 11. Implement Business Logic Layer - Recipe and Revenue Services (WITHOUT nutrition calculation)
  - [x] 11.1 Implement RecipeService
    - Implement CreateRecipeAsync with required field validation
    - Implement AddIngredientToRecipeAsync with required fields
    - Implement UpdateRecipeAsync (WITHOUT nutrition recalculation for now)
    - Implement DeleteRecipeAsync with active menu constraint check
    - _Requirements: 11.1, 11.2, 11.6_
  
  - [x] 11.2 Implement IngredientService
    - Implement CreateIngredientAsync with required fields
    - Implement allergen flag handling
    - _Requirements: 11.3, 11.4_
  
  - [x] 11.3 Write property tests for Recipe and Ingredient Services

    - **Property 51: Recipe required fields**
    - **Property 52: Recipe ingredient required fields**
    - **Property 53: Ingredient required fields**
    - **Property 55: Recipe deletion constraint**
    - **Validates: Requirements 11.1, 11.2, 11.3, 11.6**
  
  - [x] 11.4 Create DTOs for Revenue
    - Implement RevenueReportDto, DashboardStatsDto
    - _Requirements: 16.6_
  
  - [x] 11.5 Implement RevenueService
    - Implement GenerateMonthlyReportAsync with order revenue calculation (skip subscription revenue for now)
    - Implement GetMonthlyReportAsync
    - Implement GetYearlyRevenueAsync with monthly aggregation
    - Implement GetDashboardStatsAsync (skip active subscription count for now)
    - NOTE: Subscription revenue calculations deferred to checkpoint 2
    - _Requirements: 12.2, 12.3, 12.4, 12.5, 12.6, 13.1_
  
  - [ ]* 11.6 Write property tests for RevenueService
    - **Property 57: Monthly order revenue calculation**
    - **Property 58: Monthly order count**
    - **Property 59: Dashboard statistics aggregation** (partial - without subscription data)
    - **Property 60: Yearly revenue aggregation**
    - **Property 61: Revenue report persistence**
    - **Validates: Requirements 12.2, 12.3, 12.4, 12.5, 12.6**

- [x] 12. Implement exception handling and validation
  - [x] 12.1 Create custom exception classes
    - Implement BusinessException, AuthenticationException, AuthorizationException
    - Implement ValidationException, NotFoundException, ConstraintViolationException
    - _Requirements: 15.1_
  
  - [x] 12.2 Implement FluentValidation validators
    - Create validators for all DTOs
    - Implement email format validation
    - Implement positive value validation
    - Implement date validation
    - _Requirements: 15.1, 15.3, 15.4, 15.5, 15.7_
  
  - [x] 12.3 Implement global exception handler
    - Create GlobalExceptionHandler middleware
    - Map exceptions to HTTP status codes
    - Return user-friendly error messages
    - _Requirements: 15.1, 15.2_
  
  - [ ]* 12.4 Write property tests for validation
    - **Property 63: Descriptive error messages**
    - **Property 64: Past date rejection for future operations**
    - **Property 65: Negative value rejection**
    - **Property 66: Required field validation**
    - **Property 67: Foreign key constraint enforcement**
    - **Property 68: Email format validation**
    - **Validates: Requirements 15.1, 15.3, 15.4, 15.5, 15.6, 15.7**

- [x] 13. Checkpoint 2 - Ensure all business logic tests pass (excluding subscription and standalone nutrition calculator)
  - Run all service layer tests
  - Verify all property tests pass with 100+ iterations
  - Check transaction rollback behavior
  - NOTE: Subscription and standalone nutrition calculator deferred to checkpoint 2
  - NOTE: Nutrition display from existing recipe data IS included in checkpoint 1
  - Ask the user if questions arise


- [x] 14. Implement Presentation Layer - Authentication and Authorization (WITHOUT subscription checks)
  - [x] 14.1 Configure ASP.NET Core Identity and Cookie Authentication
    - Set up authentication middleware
    - Configure cookie settings
    - Set up Google OAuth (optional)
    - _Requirements: 1.3, 1.4, 1.7_
  
  - [x] 14.2 Create ViewModels for Account
    - Implement RegisterViewModel with validation attributes
    - Implement LoginViewModel with validation attributes
    - _Requirements: 16.6_
  
  - [x] 14.3 Implement AccountController
    - Implement Register GET and POST actions
    - Implement Login GET and POST actions
    - Implement Logout action
    - Implement SignInUser helper method with claims
    - _Requirements: 1.1, 1.2, 1.4, 1.5_
  
  - [x] 14.4 Create authorization filters
    - Implement role-based authorization attributes
    - NOTE: Skip RequireActiveSubscriptionAttribute for now (deferred to checkpoint 2)
    - _Requirements: 1.7, 14.1, 14.2, 14.3, 14.4, 14.5, 14.6_
  
  - [ ]* 14.5 Write unit tests for AccountController
    - Test registration with valid and invalid inputs
    - Test login with valid and invalid credentials
    - Test authorization filters
    - _Requirements: 1.1, 1.2, 1.4, 1.5_
  
  - [ ]* 14.6 Write property test for role-based authorization
    - **Property 6: Role-based authorization**
    - **Validates: Requirements 1.7, 14.1, 14.2, 14.3, 14.4, 14.5, 14.6**

- [x] 15. Implement Presentation Layer - . Profile (WITHOUT subscription display)
  - [x] 15.1 Create ViewModels for HealthProfile
    - Implement HealthProfileViewModel with validation
    - _Requirements: 16.6_
  
  - [x] 15.2 Implement HealthProfileController
    - Implement Create/Edit GET and POST actions
    - Implement AddAllergy and RemoveAllergy actions
    - Implement AddFoodPreference and RemoveFoodPreference actions
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_
  
  - [ ]* 15.3 Write unit tests for HealthProfile controller
    - Test health profile CRUD operations
    - _Requirements: 2.1_

- [x] 16. Implement Presentation Layer - Meal Plans and Fridge (WITH nutrition display)
  - [x] 16.1 Create ViewModels for MealPlan
    - Implement MealPlanViewModel, CreateMealPlanViewModel
    - Implement MealViewModel WITH nutrition display from existing recipe data
    - _Requirements: 16.6_
  
  - [x] 16.2 Implement MealPlanController
    - Implement Index action to list meal plans
    - Implement Create GET and POST actions for manual plans
    - Implement GenerateAI POST action for AI plans
    - Implement Details action WITH nutrition display from existing recipe data
    - _Requirements: 4.1, 4.2, 5.1, 5.3, 6.3, 6.4_
  
  - [x] 16.3 Create ViewModels for Fridge
    - Implement FridgeViewModel, AddFridgeItemViewModel
    - Implement GroceryListViewModel
    - _Requirements: 16.6_
  
  - [x] 16.4 Implement FridgeController
    - Implement Index action to display fridge items
    - Implement Add POST action
    - Implement UpdateQuantity POST action
    - Implement Remove POST action
    - Implement GroceryList action
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7_
  
  - [ ]* 16.5 Write unit tests for MealPlan and Fridge controllers
    - Test meal plan creation and viewing
    - Test fridge CRUD operations
    - Test grocery list generation
    - _Requirements: 5.1, 7.1, 7.5_


- [x] 17. Implement Presentation Layer - Menu and Orders (WITHOUT subscription checks)
  - [x] 17.1 Create ViewModels for Menu
    - Implement DailyMenuViewModel, MenuMealViewModel
    - Implement CreateMenuViewModel
    - _Requirements: 16.6_
  
  - [x] 17.2 Implement MenuController (Manager role)
    - Implement Index action to list menus
    - Implement Create GET and POST actions
    - Implement AddMeal POST action
    - Implement Publish POST action
    - Implement UpdateQuantity POST action
    - _Requirements: 8.1, 8.2, 8.3, 8.4_
  
  - [x] 17.3 Implement PublicMenuController (Guest/Customer role)
    - Implement Today action to view today's menu
    - Implement Weekly action to view weekly menu
    - _Requirements: 8.5, 8.6_
  
  - [x] 17.4 Create ViewModels for Order
    - Implement CreateOrderViewModel, OrderItemViewModel
    - Implement PaymentViewModel
    - Implement OrderListViewModel
    - _Requirements: 16.6_
  
  - [x] 17.5 Implement OrderController
    - Implement Index action to list customer orders
    - Implement Create GET and POST actions
    - Implement Payment GET and POST actions
    - Implement Confirmation action
    - NOTE: Skip RequireActiveSubscription filter for now (deferred to checkpoint 2)
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7_
  
  - [ ]* 17.6 Write unit tests for Menu and Order controllers
    - Test menu management workflow
    - Test order creation and payment workflow
    - _Requirements: 8.1, 9.1_

- [x] 17.7 ENHANCEMENT: Implement Enhanced Payment UI (COD and VNPAY)
  - [x] 17.7.1 Update OrderController for enhanced payment methods
    - Update Payment action to show COD and VNPAY options
    - Update ProcessPayment action to handle VNPAY redirects and COD processing
    - Implement VnpayCallback action for VNPAY payment confirmations
    - Implement PaymentFailed and PaymentError actions for error handling
    - Add proper error handling and user feedback
    - _Requirements: 9.5, 9.6, 9.7_
  
  - [x] 17.7.2 Implement DeliveryController for delivery men
    - Implement AssignedDeliveries action to show delivery man's orders
    - Implement ConfirmCashPayment action for COD payment confirmation
    - Implement CompleteDelivery action for delivery completion
    - Add proper authorization for DeliveryMan role
    - _Requirements: 10.6, 10.7, 10.4_
  
  - [x] 17.7.3 Update ViewModels for enhanced payment
    - Update PaymentViewModel with payment method options
    - Create PaymentMethodOption class for dropdown options
    - Create AssignedDeliveriesViewModel for delivery man interface
    - Create DeliveryScheduleViewModel with payment confirmation capabilities
    - _Requirements: 16.6_
  
  - [ ]* 17.7.4 Write unit tests for enhanced payment controllers
    - Test payment method selection and processing
    - Test VNPAY callback handling
    - Test delivery man cash payment confirmation
    - Test error handling for payment failures
    - _Requirements: 9.5, 9.6, 9.7, 10.6, 10.7_

- [x] 18. Implement Presentation Layer - Delivery and Admin (WITHOUT subscription data)
  - [x] 18.1 Create ViewModels for Delivery
    - Implement DeliveryScheduleViewModel
    - Implement UpdateDeliveryTimeViewModel
    - _Requirements: 16.6_
  
  - [x] 18.2 Implement DeliveryController (Customer and DeliveryMan roles)
    - Implement MyDeliveries action for customers
    - Implement AssignedDeliveries action for delivery men
    - Implement Complete POST action
    - Implement UpdateTime POST action
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5_
  
  - [x] 18.3 Create ViewModels for Admin
    - Implement AdminDashboardViewModel (WITHOUT active subscription count for now)
    - Implement RevenueReportViewModel (WITHOUT subscription revenue for now)
    - _Requirements: 16.6_
  
  - [x] 18.4 Implement AdminController
    - Implement Dashboard action with statistics (skip active subscription count for now)
    - Implement Revenue action with monthly/yearly reports (skip subscription revenue for now)
    - Implement AI configuration actions (stub)
    - NOTE: Subscription-related data deferred to checkpoint 2
    - _Requirements: 12.2, 12.3, 12.4, 12.5, 13.1, 13.2, 13.3, 13.4_
  
  - [ ]* 18.5 Write unit tests for Delivery and Admin controllers
    - Test delivery workflow
    - Test admin dashboard and reports (partial - without subscription data)
    - _Requirements: 10.1, 12.4_

- [x] 19. Implement Presentation Layer - Recipe Management
  - [x] 19.1 Create ViewModels for Recipe and Ingredient
    - Implement RecipeViewModel, CreateRecipeViewModel
    - Implement IngredientViewModel
    - _Requirements: 16.6_
  
  - [x] 19.2 Implement RecipeController (Manager role)
    - Implement Index action to list recipes
    - Implement Create GET and POST actions
    - Implement Edit GET and POST actions
    - Implement Delete POST action with constraint check
    - Implement AddIngredient POST action
    - _Requirements: 11.1, 11.2, 11.5, 11.6_
  
  - [x] 19.3 Implement IngredientController (Manager role)
    - Implement Index action to list ingredients
    - Implement Create GET and POST actions
    - _Requirements: 11.3, 11.4_
  
  - [ ]* 19.4 Write unit tests for Recipe and Ingredient controllers
    - Test recipe CRUD operations
    - Test ingredient management
    - Test recipe deletion constraint
    - _Requirements: 11.1, 11.3, 11.6_

- [ ] 20. Checkpoint 3 - Ensure all presentation layer tests pass (excluding subscription and standalone nutrition calculator)
  - Run all controller tests
  - Test authentication and authorization flows
  - Verify ViewModels validation works correctly
  - NOTE: Subscription and standalone nutrition calculator deferred to checkpoint 2
  - NOTE: Nutrition display from existing recipe data IS included in checkpoint 1
  - Ask the user if questions arise


- [x] 21. Create Views for Authentication and Account Management
  - [x] 21.1 Create layout and shared views
    - Implement _Layout.cshtml with navigation
    - Implement _LoginPartial.cshtml for user menu
    - Implement error views
    - _Requirements: 16.3_
  
  - [x] 21.2 Create Account views
    - Implement Register.cshtml with form validation
    - Implement Login.cshtml with form validation
    - _Requirements: 1.1, 1.4_
  
  - [x] 21.3 Create HealthProfile views
    - Implement Create/Edit.cshtml with allergy and preference selection
    - _Requirements: 2.1, 2.2, 2.4_

- [x] 22. Create Views for Meal Planning and Fridge (WITH nutrition display)
  - [x] 22.1 Create MealPlan views
    - Implement Index.cshtml to list meal plans
    - Implement Create.cshtml for manual plan creation
    - Implement Details.cshtml WITH nutrition display from existing recipe data
    - _Requirements: 5.1, 6.3, 6.4_
  
  - [x] 22.2 Create Fridge views
    - Implement Index.cshtml to display fridge items with expiry highlighting
    - Implement Add.cshtml for adding items
    - Implement GroceryList.cshtml to show missing ingredients
    - _Requirements: 7.1, 7.4, 7.5, 7.6, 7.7_

- [x] 23. Create Views for Menu and Orders
  - [x] 23.1 Create Menu views (Manager)
    - Implement Index.cshtml to list menus
    - Implement Create.cshtml for menu creation
    - Implement AddMeal.cshtml for adding meals to menu
    - _Requirements: 8.1, 8.2_
  
  - [x] 23.2 Create PublicMenu views (Guest/Customer)
    - Implement Today.cshtml to display today's menu
    - Implement Weekly.cshtml to display weekly menu
    - _Requirements: 8.5, 8.6_
  
  - [x] 23.3 Create Order views
    - Implement Index.cshtml to list customer orders
    - Implement Create.cshtml for order creation
    - Implement Payment.cshtml for payment processing
    - Implement Confirmation.cshtml for order confirmation
    - _Requirements: 9.1, 9.5, 9.7_

- [x] 23.4 ENHANCEMENT: Create Enhanced Payment Views (COD and VNPAY)
  - [x] 23.4.1 Update Order views for enhanced payment
    - Update Payment.cshtml with payment method selection (COD/VNPAY radio buttons)
    - Update Confirmation.cshtml to show payment method and status
    - Create PaymentFailed.cshtml for failed payment handling
    - Create PaymentError.cshtml for payment error display
    - Add proper styling and user guidance for payment options
    - _Requirements: 9.5, 9.6, 9.7_
  
  - [x] 23.4.2 Create Delivery views for delivery men
    - Create AssignedDeliveries.cshtml to show delivery man's assigned orders
    - Include cash payment confirmation buttons for COD orders
    - Include delivery completion buttons for confirmed orders
    - Show order details, payment status, and customer information
    - Add proper styling and responsive design for mobile use
    - _Requirements: 10.6, 10.7, 10.4_
  
  - [x] 23.4.3 Add payment status indicators
    - Create partial views for order status badges
    - Add payment method icons (cash, credit card)
    - Create progress indicators for order workflow
    - Add tooltips and help text for payment processes
    - _Requirements: 9.5, 9.6, 9.7, 10.6_

- [x] 24. Create Views for Delivery, Recipe, and Admin (WITHOUT subscription data)
  - [x] 24.1 Create Delivery views
    - Implement MyDeliveries.cshtml for customers
    - Implement AssignedDeliveries.cshtml for delivery men
    - _Requirements: 10.2, 10.3_
  
  - [x] 24.2 Create Recipe views (Manager)
    - Implement Index.cshtml to list recipes
    - Implement Create.cshtml for recipe creation
    - Implement Edit.cshtml for recipe editing
    - _Requirements: 11.1, 11.2_
  
  - [x] 24.3 Create Ingredient views (Manager)
    - Implement Index.cshtml to list ingredients
    - Implement Create.cshtml for ingredient creation
    - _Requirements: 11.3, 11.4_
  
  - [x] 24.4 Create Admin views
    - Implement Dashboard.cshtml with statistics (skip active subscription count for now)
    - Implement Revenue.cshtml with monthly/yearly reports (skip subscription revenue for now)
    - NOTE: Subscription-related data deferred to checkpoint 2
    - _Requirements: 12.4, 12.5, 13.1_


- [x] 25. Configure dependency injection and startup (WITHOUT subscription services)
  - [x] 25.1 Register all services in Program.cs
    - Register DbContext with connection string
    - Register all repositories
    - Register UnitOfWork
    - Register all business services (skip SubscriptionService and NutritionCalculatorService for now)
    - Register VnpayService for payment processing
    - Register authentication services
    - Register FluentValidation
    - NOTE: Subscription and nutrition services deferred to checkpoint 2
    - _Requirements: 16.8_
  
  - [x] 25.2 Configure middleware pipeline
    - Add authentication middleware
    - Add authorization middleware
    - Add global exception handler
    - Add static files and routing
    - _Requirements: 1.7, 15.1_
  
  - [x] 25.3 Create database seeding
    - Seed sample ingredients
    - Seed admin account
    - NOTE: Skip subscription packages for now (deferred to checkpoint 2)
    - _Requirements: None_

- [x] 25.4 ENHANCEMENT: Configure VNPAY Integration
  - [x] 25.4.1 Add VNPAY configuration settings
    - Add VnPay section to appsettings.json with Url, TmnCode, HashSecret, ReturnUrl
    - Add VnPay section to appsettings.Development.json for testing
    - Configure VNPAY sandbox/production URLs based on environment
    - Add proper configuration validation and error handling
    - _Requirements: 9.6_
  
  - [x] 25.4.2 Register VNPAY service dependencies
    - Register IVnpayService with VnpayService implementation
    - Configure VNPAY service with proper scoped lifetime
    - Add VNPAY configuration to dependency injection
    - _Requirements: 9.6, 16.8_

- [ ] 26. Integration testing and end-to-end workflows (excluding subscription and standalone nutrition calculator)
  - [ ]* 26.1 Write integration test for complete order workflow
    - Test: Register → View Menu → Create Order → Payment → Delivery
    - NOTE: Skip subscription check for now (deferred to checkpoint 2)
    - _Requirements: 1.1, 8.5, 9.1, 9.7, 10.1_
  
  - [ ]* 26.2 Write integration test for meal plan workflow
    - Test: Create Health Profile → Generate AI Meal Plan → View Nutrition
    - NOTE: Skip subscription check for now (deferred to checkpoint 2)
    - NOTE: Nutrition display from existing recipe data IS included
    - _Requirements: 2.1, 4.2, 6.3, 6.4_
  
  - [ ]* 26.3 Write integration test for fridge workflow
    - Test: Add Fridge Items → Create Meal Plan → Generate Grocery List
    - _Requirements: 7.1, 5.1, 7.5_
  
  - [ ]* 26.4 Write integration test for admin workflow
    - Test: View Dashboard → Generate Revenue Report (WITHOUT subscription data)
    - NOTE: Skip subscription revenue for now (deferred to checkpoint 2)
    - _Requirements: 12.4, 12.2_

- [ ]* 26.5 ENHANCEMENT: Integration tests for enhanced payment system
  - [ ]* 26.5.1 Write integration test for COD payment workflow
    - Test: Create Order → Select COD → Confirm Order → Delivery Man Confirms Payment → Complete Delivery
    - Verify order status transitions: pending → pending_payment → confirmed → delivered
    - Test delivery man authorization for payment confirmation
    - _Requirements: 9.5, 9.10, 10.6, 10.7_
  
  - [ ]* 26.5.2 Write integration test for VNPAY payment workflow
    - Test: Create Order → Select VNPAY → Redirect to Gateway → Process Callback → Confirm Order
    - Mock VNPAY callback responses for success and failure scenarios
    - Verify secure hash validation and order status updates
    - Test inventory restoration on payment failure
    - _Requirements: 9.6, 9.7, 9.8_
  
  - [ ]* 26.5.3 Write integration test for payment error handling
    - Test invalid payment methods, expired orders, unauthorized access
    - Test VNPAY callback validation failures
    - Test delivery man role restrictions for cash confirmation
    - Verify proper error messages and user feedback
    - _Requirements: 9.5, 9.6, 10.6, 10.7_

- [ ] 27. Final checkpoint 1 - Complete system validation (excluding subscription and standalone nutrition calculator)
  - Run all unit tests, property tests, and integration tests
  - Verify all applicable correctness properties pass with 100+ iterations
  - Test all user workflows manually
  - Verify database migrations and seeding
  - Check error handling and validation across all layers
  - Ensure proper authorization for all roles
  - NOTE: Subscription and standalone nutrition calculator deferred to checkpoint 2
  - NOTE: Nutrition display from existing recipe data IS included in checkpoint 1
  - Ask the user if questions arise

## Checkpoint 2 Features (DEFERRED)

The following features are deferred to checkpoint 2:
- Task 5: Subscription Management (Requirements 3.x)
- Task 6: Standalone Nutrition Calculator Service (Requirements 6.1, 6.2)
  - This is the feature where users manually input ingredients to calculate nutrition
  - Nutrition display from existing recipe data IS included in checkpoint 1
- Related subscription checks in other tasks

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Property tests validate universal correctness properties with 100+ iterations
- Unit tests validate specific examples and edge cases
- Integration tests validate end-to-end workflows
- Checkpoints ensure incremental validation at key milestones
- The implementation follows strict 3-layer architecture with no layer violations
- All cross-layer communication uses DTOs/ViewModels
- Transaction management ensures data consistency for multi-step operations
