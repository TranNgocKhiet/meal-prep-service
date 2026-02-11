# Swagger API Documentation

Swagger/OpenAPI documentation has been added to the Meal Prep Service web application with comprehensive REST API endpoints.

## API Controllers

The application now includes 10 API controllers covering all major functionality:

1. **AccountApi** - User registration, authentication, and staff management
2. **MenuApi** - Daily and weekly menu access
3. **RecipeApi** - Recipe browsing and filtering
4. **OrderApi** - Order creation, payment processing, and status management
5. **MealPlanApi** - AI-generated and manual meal plan management
6. **IngredientApi** - Ingredient CRUD operations
7. **AllergyApi** - Allergy management
8. **HealthProfileApi** - User health profile and dietary restrictions
9. **FridgeApi** - Fridge inventory and grocery list generation
10. **DeliveryApi** - Delivery scheduling and tracking

## Accessing Swagger UI

Once the application is running, you can access the Swagger UI at:

**HTTPS:**
```
https://localhost:7073/swagger
```

**HTTP:**
```
http://localhost:5020/swagger
```

## Available API Endpoints

### Account API (`/api/AccountApi`)
- **POST /api/AccountApi/register** - Register a new customer account
- **POST /api/AccountApi/login** - Authenticate user
- **GET /api/AccountApi/{id}** - Get account by ID
- **GET /api/AccountApi/email-exists/{email}** - Check if email exists
- **GET /api/AccountApi/staff** - Get all staff accounts (Admin only)
- **GET /api/AccountApi/role/{role}** - Get accounts by role
- **POST /api/AccountApi/staff** - Create staff account (Admin only)
- **PUT /api/AccountApi/staff/{id}** - Update staff account (Admin only)
- **DELETE /api/AccountApi/staff/{id}** - Delete staff account (Admin only)

### Menu API (`/api/MenuApi`)
- **GET /api/MenuApi/today** - Get today's menu
- **GET /api/MenuApi/date/{date}** - Get menu by specific date (format: yyyy-MM-dd)
- **GET /api/MenuApi/weekly** - Get weekly menu (next 7 days from specified start date)

### Recipe API (`/api/RecipeApi`)
- **GET /api/RecipeApi** - Get all recipes
- **GET /api/RecipeApi/with-ingredients** - Get all recipes with their ingredients
- **GET /api/RecipeApi/{id}** - Get recipe by ID (GUID)
- **GET /api/RecipeApi/by-ingredients** - Get recipes by ingredient IDs (comma-separated GUIDs)
- **GET /api/RecipeApi/excluding-allergens** - Get recipes excluding specific allergens (comma-separated GUIDs)

### Order API (`/api/OrderApi`)
- **POST /api/OrderApi** - Create a new order
- **GET /api/OrderApi/{id}** - Get order by ID
- **GET /api/OrderApi/account/{accountId}** - Get orders by account ID
- **POST /api/OrderApi/{id}/payment** - Process payment for an order
- **POST /api/OrderApi/{id}/confirm-cash** - Confirm cash payment (Delivery Man only)
- **PUT /api/OrderApi/{id}/status** - Update order status
- **POST /api/OrderApi/vnpay-callback** - Process VNPay callback

### Meal Plan API (`/api/MealPlanApi`)
- **POST /api/MealPlanApi/generate-ai** - Generate AI meal plan
- **POST /api/MealPlanApi/manual** - Create manual meal plan
- **GET /api/MealPlanApi/{id}** - Get meal plan by ID
- **GET /api/MealPlanApi/account/{accountId}** - Get meal plans by account ID
- **GET /api/MealPlanApi/account/{accountId}/active** - Get active meal plan for account
- **POST /api/MealPlanApi/{id}/meals** - Add meal to plan
- **POST /api/MealPlanApi/{id}/activate** - Set meal plan as active
- **DELETE /api/MealPlanApi/meals/{mealId}/recipes/{recipeId}** - Remove recipe from meal
- **PUT /api/MealPlanApi/meals/{mealId}/finished** - Mark meal as finished/unfinished
- **DELETE /api/MealPlanApi/{id}** - Delete meal plan

### Ingredient API (`/api/IngredientApi`)
- **GET /api/IngredientApi** - Get all ingredients
- **GET /api/IngredientApi/{id}** - Get ingredient by ID
- **GET /api/IngredientApi/allergens** - Get all allergens
- **POST /api/IngredientApi** - Create new ingredient
- **PUT /api/IngredientApi/{id}** - Update ingredient
- **DELETE /api/IngredientApi/{id}** - Delete ingredient

### Allergy API (`/api/AllergyApi`)
- **GET /api/AllergyApi** - Get all allergies
- **GET /api/AllergyApi/{id}** - Get allergy by ID
- **POST /api/AllergyApi** - Create new allergy
- **PUT /api/AllergyApi/{id}** - Update allergy
- **DELETE /api/AllergyApi/{id}** - Delete allergy

### Health Profile API (`/api/HealthProfileApi`)
- **GET /api/HealthProfileApi/account/{accountId}** - Get health profile by account ID
- **POST /api/HealthProfileApi** - Create or update health profile
- **POST /api/HealthProfileApi/{profileId}/allergies/{allergyId}** - Add allergy to health profile
- **DELETE /api/HealthProfileApi/{profileId}/allergies/{allergyId}** - Remove allergy from health profile

### Fridge API (`/api/FridgeApi`)
- **GET /api/FridgeApi/account/{accountId}** - Get all fridge items for account
- **GET /api/FridgeApi/account/{accountId}/paged** - Get fridge items with pagination
- **GET /api/FridgeApi/account/{accountId}/expiring** - Get expiring items for account
- **POST /api/FridgeApi** - Add item to fridge
- **PUT /api/FridgeApi/{itemId}/quantity** - Update item quantity
- **PUT /api/FridgeApi/{itemId}/expiry** - Update item expiry date
- **DELETE /api/FridgeApi/{itemId}** - Remove item from fridge
- **POST /api/FridgeApi/account/{accountId}/grocery-list** - Generate grocery list from meal plan
- **POST /api/FridgeApi/account/{accountId}/grocery-list/active** - Generate grocery list from active meal plan

### Delivery API (`/api/DeliveryApi`)
- **POST /api/DeliveryApi** - Create delivery schedule
- **GET /api/DeliveryApi/account/{accountId}** - Get deliveries by account ID
- **GET /api/DeliveryApi/deliveryman/{deliveryManId}** - Get deliveries by delivery man ID
- **POST /api/DeliveryApi/{deliveryId}/complete** - Complete delivery
- **PUT /api/DeliveryApi/{deliveryId}/time** - Update delivery time

## Features

- **Interactive API Documentation** - Test API endpoints directly from the browser
- **Request/Response Examples** - See example requests and responses for each endpoint
- **Schema Definitions** - View detailed DTO schemas
- **Authentication Support** - Cookie-based authentication is documented
- **Dark Theme** - Swagger UI is configured with a clean, professional appearance

## Running the Application

1. Build the project:
   ```bash
   dotnet build
   ```

2. Run the application:
   ```bash
   dotnet run --project src/MealPrepService.Web
   ```

3. Navigate to the Swagger UI URL in your browser

## Notes

- Swagger is enabled in all environments (Development, Staging, Production)
- API controllers are located in `src/MealPrepService.Web/PresentationLayer/Controllers/Api/`
- All API endpoints return JSON responses
- Error responses include descriptive error messages
