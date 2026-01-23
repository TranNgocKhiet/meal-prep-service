# Requirements Document: Meal Prep Service Application

## Introduction

The Meal Prep Service Application is a comprehensive meal preparation and delivery platform built with ASP.NET Core MVC using a 3-layer architecture. The system serves multiple user roles (Admin, Manager, Customer, Guest, Delivery Man, AI Agent) and provides subscription-based meal planning with AI-generated options, daily menu management, virtual fridge tracking, order processing, and delivery management.

## Glossary

// This is web app, not application
- **System**: The Meal Prep Service Application
- **Account_Manager**: The component responsible for user authentication and authorization
- **Health_Profile_Manager**: The component managing customer health information
- **Subscription_Manager**: The component handling subscription packages and user subscriptions
- **Meal_Plan_Generator**: The AI-powered component that creates personalized meal plans
- **Nutrition_Calculator**: The AI-powered component that calculates meal nutritional values
- **Menu_Manager**: The component managing daily and weekly menus
- **Fridge_Manager**: The component managing virtual fridge inventory
- **Order_Processor**: The component handling order creation and payment
- **Delivery_Tracker**: The component managing delivery schedules
- **Revenue_Analyzer**: The component generating financial reports
// Guest can use the nutrient caculator
- **Guest**: An unauthenticated user with limited access
// Only customer can use the create meal plan function but with limit access (3 times) and will increase with subcription
- **Customer**: An authenticated user who can order meals and manage subscriptions
- **Manager**: A staff member who manages menus and meal plans
- **Admin**: A system administrator with full access and AI configuration rights
- **Delivery_Man**: A user who handles meal deliveries
- **AI_Agent**: An automated system component that performs AI-powered operations
- **Valid_Email**: An email address matching standard email format (RFC 5322)
// There is a function called upgrade subcription, like if the user upgrade from a plan to a more expensive plan, the system will discount the new plan price base on current plan remaining days
- **Active_Subscription**: A subscription with status "active" and end_date >= current_date
- **Meal_Plan**: A collection of meals organized by date and meal type
- **Daily_Menu**: A menu containing available meals for a specific date
- **Virtual_Fridge**: A customer's inventory of ingredients with quantities and expiry dates
- **Order**: A customer's purchase of meals from the daily menu with payment method specification
- **COD_Order**: An order with payment method "Cash on Delivery" requiring cash payment confirmation by delivery man
- **VNPAY_Order**: An order with payment method "VNPAY" processed through Vietnamese payment gateway
- **Payment_Gateway**: External service (VNPAY) that processes online payments and sends confirmation callbacks
- **Delivery_Schedule**: A scheduled delivery with time, address, and driver information

## Requirements

### Requirement 1: User Authentication and Authorization

**User Story:** As a guest, I want to create an account using email/password or Google OAuth, so that I can access customer features.

#### Acceptance Criteria

1. WHEN a guest provides a valid email, password, and full name, THE Account_Manager SHALL create a new account with role "Customer"
2. WHEN a guest provides an email that already exists, THE Account_Manager SHALL reject the registration and return an error message
3. WHEN a guest initiates Google OAuth authentication, THE Account_Manager SHALL create or retrieve an account using the Google profile information
4. WHEN a user provides valid credentials, THE Account_Manager SHALL authenticate the user and create a session
5. WHEN a user provides invalid credentials, THE Account_Manager SHALL reject the authentication and return an error message
6. THE Account_Manager SHALL hash all passwords before storing them in the database
7. WHEN a user's session is active, THE System SHALL authorize access based on the user's role

### Requirement 2: Health Profile Management

**User Story:** As a customer, I want to manage my health profile including allergies and food preferences, so that the system can generate appropriate meal plans.

#### Acceptance Criteria

1. WHEN a customer creates a health profile, THE Health_Profile_Manager SHALL store age, weight, height, gender, and optional health notes
2. WHEN a customer adds an allergy, THE Health_Profile_Manager SHALL link the allergy to the customer's health profile
3. WHEN a customer removes an allergy, THE Health_Profile_Manager SHALL unlink the allergy from the customer's health profile
4. WHEN a customer adds a food preference, THE Health_Profile_Manager SHALL link the preference to the customer's health profile
5. WHEN a customer removes a food preference, THE Health_Profile_Manager SHALL unlink the preference from the customer's health profile
6. WHEN a customer updates health profile information, THE Health_Profile_Manager SHALL validate that weight and height are positive numbers
7. WHEN a customer updates health profile information, THE Health_Profile_Manager SHALL validate that age is between 1 and 150

### Requirement 3: Subscription Management

**User Story:** As a customer, I want to subscribe to meal packages, so that I can access meal planning and ordering features.

#### Acceptance Criteria

1. WHEN a guest or customer views subscription packages, THE Subscription_Manager SHALL display all available packages with name, price, duration, and description
2. WHEN a customer selects a subscription package, THE Subscription_Manager SHALL create a user subscription with start_date as current date and end_date as start_date plus duration_days
3. WHEN a customer subscribes, THE Subscription_Manager SHALL set the subscription status to "active"
4. WHEN a subscription's end_date is reached, THE Subscription_Manager SHALL update the subscription status to "expired"
5. WHEN a customer attempts to access subscription features without an active subscription, THE System SHALL deny access and prompt for subscription
6. WHERE AI price adjustment is enabled, THE Subscription_Manager SHALL allow admins to modify package prices

### Requirement 4: AI-Powered Meal Plan Generation

**User Story:** As a customer, I want the system to generate personalized meal plans based on my health profile, so that I receive nutritionally appropriate meals.

#### Acceptance Criteria

1. WHEN a customer requests an AI-generated meal plan, THE Meal_Plan_Generator SHALL retrieve the customer's health profile, allergies, and food preferences
2. WHEN generating a meal plan, THE Meal_Plan_Generator SHALL create meals for the specified date range with meal types (breakfast, lunch, dinner)
3. WHEN generating a meal plan, THE Meal_Plan_Generator SHALL exclude recipes containing ingredients that match the customer's allergies
4. WHEN generating a meal plan, THE Meal_Plan_Generator SHALL prioritize recipes matching the customer's food preferences
5. WHEN a meal plan is generated, THE Meal_Plan_Generator SHALL set is_ai_generated to true
6. WHEN a meal plan is created, THE System SHALL associate it with the customer's account
7. WHERE a customer has an active subscription, THE Meal_Plan_Generator SHALL allow meal plan creation

### Requirement 5: Manual Meal Plan Creation

**User Story:** As a customer or manager, I want to create custom meal plans manually, so that I can have control over my meal selections.

#### Acceptance Criteria

1. WHEN a user creates a manual meal plan, THE System SHALL accept a plan name, start date, and end date
2. WHEN creating a manual meal plan, THE System SHALL set is_ai_generated to false
3. WHEN a user adds a meal to a plan, THE System SHALL require meal_type and serve_date within the plan's date range
4. WHEN a user adds recipes to a meal, THE System SHALL link the selected recipes to the meal
5. WHEN a manager edits a customer's meal plan, THE System SHALL allow modification of meals and recipes
6. WHEN a customer adds a personal recipe, THE System SHALL allow specification of ingredients and portions

### Requirement 6: Nutrition Calculation

**User Story:** As a customer, I want to view nutritional information for my meals, so that I can track my dietary intake.

#### Acceptance Criteria

1. WHEN a recipe is created or updated, THE Nutrition_Calculator SHALL compute total_calories, protein_g, fat_g, and carbs_g based on recipe ingredients
2. WHEN calculating nutrition, THE Nutrition_Calculator SHALL multiply each ingredient's calo_per_unit by the amount specified in recipe_ingredient
3. WHEN a customer views a meal, THE System SHALL display the sum of nutritional values from all recipes in that meal
4. WHEN a customer views a meal plan, THE System SHALL display daily and total nutritional summaries
5. WHERE AI calculation is enabled, THE Nutrition_Calculator SHALL use the configured AI model version

### Requirement 7: Virtual Fridge Management

**User Story:** As a customer, I want to manage a virtual fridge of ingredients, so that I can track what I have and generate grocery lists.

#### Acceptance Criteria

1. WHEN a customer adds an ingredient to the fridge, THE Fridge_Manager SHALL store the ingredient, current_amount, and expiry_date
2. WHEN a customer updates a fridge item quantity, THE Fridge_Manager SHALL validate that current_amount is non-negative
3. WHEN a customer removes a fridge item, THE Fridge_Manager SHALL delete the fridge item record
4. WHEN a customer views the fridge, THE Fridge_Manager SHALL display all items with quantities and expiry dates
5. WHEN a customer requests a grocery list, THE Fridge_Manager SHALL compare meal plan ingredients with fridge inventory and return missing or insufficient ingredients
6. WHEN a fridge item's expiry_date is within 3 days, THE System SHALL highlight the item as expiring soon
7. WHEN a fridge item's expiry_date has passed, THE System SHALL mark the item as expired

### Requirement 8: Daily and Weekly Menu Management

**User Story:** As a manager, I want to create and manage daily menus with available meals, so that customers can order from current offerings.

#### Acceptance Criteria

1. WHEN a manager creates a daily menu, THE Menu_Manager SHALL require a menu_date and set initial status to "draft"
2. WHEN a manager adds a meal to a menu, THE Menu_Manager SHALL require a recipe, price, and available_quantity
3. WHEN a manager publishes a menu, THE Menu_Manager SHALL update the status to "active"
4. WHEN a manager updates a menu meal's available quantity, THE Menu_Manager SHALL validate that the quantity is non-negative
5. WHEN a guest or customer views menus, THE System SHALL display only menus with status "active"
6. WHEN viewing a weekly menu, THE System SHALL display all active menus for the next 7 days
7. WHEN a menu meal's available_quantity reaches zero, THE System SHALL mark it as sold out

### Requirement 9: Order Processing and Payment

**User Story:** As a customer, I want to order meals from the daily menu and pay for them using various payment methods including cash on delivery and online payment, so that I can receive meal deliveries with flexible payment options.

#### Acceptance Criteria

1. WHEN a customer adds menu meals to an order, THE Order_Processor SHALL create order details with quantity and unit_price
2. WHEN creating an order, THE Order_Processor SHALL validate that each menu meal has sufficient available_quantity
3. WHEN an order is placed, THE Order_Processor SHALL calculate total_amount as the sum of (quantity × unit_price) for all order details
4. WHEN an order is placed, THE Order_Processor SHALL reduce each menu meal's available_quantity by the ordered quantity
5. WHEN a customer selects Cash on Delivery (COD) payment method, THE Order_Processor SHALL set order status to "pending_payment" and create a delivery schedule
6. WHEN a customer selects VNPAY online payment method, THE Order_Processor SHALL redirect to VNPAY gateway for payment processing
7. WHEN VNPAY payment succeeds, THE Order_Processor SHALL receive payment confirmation callback and set order status to "confirmed"
8. WHEN VNPAY payment fails, THE Order_Processor SHALL receive payment failure callback and set order status to "payment_failed"
9. IF any payment fails, THEN THE Order_Processor SHALL restore menu meal quantities to their original values
10. WHEN a delivery man confirms cash payment receipt for COD orders, THE Order_Processor SHALL update order status from "pending_payment" to "confirmed"
11. WHEN payment succeeds (VNPAY) or cash is confirmed (COD), THE Order_Processor SHALL set order status to "confirmed" and ensure delivery schedule exists
12. WHEN an order status changes to "confirmed", THE Order_Processor SHALL trigger order fulfillment workflow
13. WHERE a customer has an active subscription, THE Order_Processor SHALL allow order placement

### Requirement 10: Delivery Schedule Management

**User Story:** As a customer, I want to view my delivery schedule, so that I know when my meals will arrive.

#### Acceptance Criteria

1. WHEN an order is confirmed, THE Delivery_Tracker SHALL create a delivery schedule with delivery_time, address, and driver_contact
2. WHEN a customer views delivery schedules, THE Delivery_Tracker SHALL display all scheduled deliveries for the customer's orders
3. WHEN a delivery man views delivery schedules, THE Delivery_Tracker SHALL display all deliveries assigned to that delivery man
4. WHEN a delivery is completed, THE Delivery_Tracker SHALL update the associated order status to "delivered"
5. WHEN a delivery time is updated, THE Delivery_Tracker SHALL validate that delivery_time is in the future
6. WHEN a delivery man confirms cash payment for COD orders, THE Delivery_Tracker SHALL update the order status from "pending_payment" to "confirmed"
7. WHEN a delivery man attempts to confirm cash payment, THE Delivery_Tracker SHALL validate that the order payment method is "COD" and status is "pending_payment"

### Requirement 11: Recipe and Ingredient Management

**User Story:** As a manager, I want to manage recipes and ingredients, so that the system has accurate meal information.

#### Acceptance Criteria

1. WHEN a manager creates a recipe, THE System SHALL require recipe_name and instructions
2. WHEN a manager adds an ingredient to a recipe, THE System SHALL require the ingredient and amount
3. WHEN a manager creates an ingredient, THE System SHALL require ingredient_name, unit, and calo_per_unit
4. WHEN creating an ingredient, THE System SHALL allow marking it as an allergen with is_allergen flag
5. WHEN a manager updates a recipe, THE Nutrition_Calculator SHALL recalculate nutritional values
6. WHEN a manager deletes a recipe, THE System SHALL prevent deletion if the recipe is used in active menu meals

### Requirement 12: Revenue Reporting and Analytics

**User Story:** As an admin, I want to view revenue reports and analytics, so that I can monitor business performance.

#### Acceptance Criteria

1. WHEN an admin requests a monthly revenue report, THE Revenue_Analyzer SHALL calculate total_subscription_rev from subscriptions created in that month
2. WHEN calculating monthly revenue, THE Revenue_Analyzer SHALL calculate total_order_rev from orders placed in that month
3. WHEN generating a revenue report, THE Revenue_Analyzer SHALL count total_orders_count for the specified month
4. WHEN an admin views the dashboard, THE System SHALL display current month revenue, order count, and active subscription count
5. WHEN an admin requests yearly revenue, THE Revenue_Analyzer SHALL sum monthly reports for all 12 months
6. WHEN a revenue report is generated, THE System SHALL store it with month, year, and calculated values

### Requirement 13: Admin Dashboard and AI Configuration

**User Story:** As an admin, I want to configure AI models and view system analytics, so that I can optimize system performance.

#### Acceptance Criteria

1. WHEN an admin views the dashboard, THE System SHALL display total customers, active subscriptions, pending orders, and current month revenue
2. WHEN an admin updates the AI model version, THE System SHALL apply the new version to subsequent AI operations
3. WHEN an admin adjusts subscription prices using AI, THE System SHALL update package prices based on AI recommendations
4. WHEN an admin views AI configuration, THE System SHALL display current model version and last update timestamp
5. WHERE AI features are enabled, THE System SHALL log all AI operations for audit purposes

### Requirement 14: Role-Based Access Control

**User Story:** As a system administrator, I want to enforce role-based access control, so that users can only access features appropriate to their role.

#### Acceptance Criteria

1. WHEN a guest accesses the system, THE System SHALL allow viewing daily menus and subscription packages only
2. WHEN a customer accesses the system, THE System SHALL allow all guest features plus health profile, meal plans, fridge, and ordering features
3. WHEN a manager accesses the system, THE System SHALL allow menu management and meal plan editing features
4. WHEN an admin accesses the system, THE System SHALL allow all features including AI configuration and revenue reports
5. WHEN a delivery man accesses the system, THE System SHALL allow viewing assigned delivery schedules only
6. WHEN a user attempts to access unauthorized features, THE System SHALL deny access and return an authorization error

### Requirement 15: Data Validation and Error Handling

**User Story:** As a developer, I want comprehensive data validation and error handling, so that the system maintains data integrity.

#### Acceptance Criteria

1. WHEN any component receives invalid input, THE System SHALL return a descriptive error message without exposing internal details
2. WHEN a database operation fails, THE System SHALL log the error and return a user-friendly error message
3. WHEN a user provides a date in the past for future operations, THE System SHALL reject the input and return an error
4. WHEN a user provides negative values for quantities or prices, THE System SHALL reject the input and return an error
5. WHEN a required field is missing, THE System SHALL return an error specifying which fields are required
6. WHEN a foreign key constraint would be violated, THE System SHALL prevent the operation and return an error
7. THE System SHALL validate all email addresses against standard email format before storing

### Requirement 16: Three-Layer Architecture Implementation

**User Story:** As a developer, I want the system to follow a clean 3-layer architecture, so that the codebase is maintainable and testable.

#### Acceptance Criteria

1. THE System SHALL implement a Data Access Layer using Entity Framework Core with Repository pattern
2. THE System SHALL implement a Business Logic Layer containing service classes for business rules
3. THE System SHALL implement a Presentation Layer using ASP.NET Core MVC with controllers and views
4. WHEN a controller needs data, THE controller SHALL call business layer services, not data access repositories directly
5. WHEN a business service needs data, THE service SHALL call data access repositories through dependency injection
6. THE System SHALL use DTOs or ViewModels for data transfer between layers
7. THE System SHALL implement Unit of Work pattern for transaction management across repositories
8. THE System SHALL use dependency injection for all cross-layer dependencies
