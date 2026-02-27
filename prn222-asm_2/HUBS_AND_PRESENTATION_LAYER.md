# Hubs and PresentationLayer Documentation

This document explains the code in the `Hubs` folder and `PresentationLayer` folder in the MealPrepService.Web project.

---

## Hubs Folder

The `Hubs` folder contains SignalR Hub classes that enable real-time, bidirectional communication between the server and connected clients (browsers). SignalR uses WebSockets (with fallback to other transports) to push updates instantly without requiring page refresh.

### Hub Files Overview

| File | Purpose | Authentication |
|------|---------|----------------|
| `MenuHub.cs` | Real-time menu updates | Public (no auth required) |
| `OrderHub.cs` | Order status notifications | Requires authentication |
| `DeliveryHub.cs` | Delivery tracking updates | Requires authentication |
| `NotificationHub.cs` | General user notifications | Requires authentication |

---

### MenuHub.cs

Handles real-time menu-related updates. This hub is **public** (no authentication required) so customers can receive menu updates without logging in.

**Methods:**

| Method | Parameters | Description |
|--------|------------|-------------|
| `SendMenuUpdate` | `menuDate`, `mealId`, `quantityRemaining` | Broadcasts when meal quantity changes |
| `SendMenuAvailabilityAlert` | `menuDate`, `mealId`, `mealName`, `isAvailable` | Notifies when a meal becomes available/unavailable |
| `SendMenuStatusChange` | `menuId`, `menuDate`, `isActive` | Notifies when manager activates/deactivates a menu |

**Client Events:**
- `ReceiveMenuUpdate` - Receive quantity updates
- `ReceiveMenuAvailabilityAlert` - Receive availability changes
- `ReceiveMenuStatusChange` - Receive menu activation/deactivation

---

### OrderHub.cs

Handles real-time order status updates. Requires authentication.

**Methods:**

| Method | Parameters | Description |
|--------|------------|-------------|
| `JoinOrderGroup` | `orderId` | Subscribe to updates for a specific order |
| `LeaveOrderGroup` | `orderId` | Unsubscribe from order updates |
| `SendOrderStatusUpdate` | `orderId`, `status`, `message` | Send status update to order subscribers |

**Client Events:**
- `ReceiveOrderStatusUpdate` - Receive order status changes

---

### DeliveryHub.cs

Handles real-time delivery tracking. Requires authentication.

**Methods:**

| Method | Parameters | Description |
|--------|------------|-------------|
| `JoinDeliveryGroup` | `deliveryId` | Subscribe to delivery updates |
| `LeaveDeliveryGroup` | `deliveryId` | Unsubscribe from delivery updates |
| `SendDeliveryUpdate` | `deliveryId`, `status`, `location`, `message` | Send delivery status/location update |

**Client Events:**
- `ReceiveDeliveryUpdate` - Receive delivery status and location updates

---

### NotificationHub.cs

General-purpose notification hub for user-specific or broadcast messages. Requires authentication.

**Methods:**

| Method | Parameters | Description |
|--------|------------|-------------|
| `JoinUserGroup` | `userId` | Subscribe to user-specific notifications |
| `LeaveUserGroup` | `userId` | Unsubscribe from user notifications |
| `SendNotification` | `userId`, `message`, `type` | Send notification to specific user |
| `BroadcastNotification` | `message`, `type` | Send notification to all connected users |

**Client Events:**
- `ReceiveNotification` - Receive notification with message and type

---

## PresentationLayer Folder

The `PresentationLayer` folder contains middleware that handles cross-cutting concerns.

### Folder Structure

```
PresentationLayer/
└── Middleware/
    ├── GlobalExceptionHandler.cs
    └── GlobalExceptionHandlerExtensions.cs
```

---

## Middleware

### GlobalExceptionHandler.cs

Centralized exception handling for the entire application. Catches unhandled exceptions and returns consistent error responses.

**Exception to Status Code Mapping:**

| Exception Type | HTTP Status Code |
|----------------|------------------|
| `ArgumentNullException` | 400 Bad Request |
| `ArgumentException` | 400 Bad Request |
| `UnauthorizedAccessException` | 401 Unauthorized |
| `KeyNotFoundException` | 404 Not Found |
| Other exceptions | 500 Internal Server Error |

### GlobalExceptionHandlerExtensions.cs

Extension method to register the global exception handler in `Program.cs`:

```csharp
app.UseGlobalExceptionHandler();
```

---

## SignalR Hub Endpoints

| Hub | Endpoint |
|-----|----------|
| OrderHub | `/hubs/order` |
| DeliveryHub | `/hubs/delivery` |
| MenuHub | `/hubs/menu` |
| NotificationHub | `/hubs/notification` |

---

## Key Points

1. **SignalR Hubs** enable real-time updates without page refresh
2. **Global Exception Handler** ensures consistent error responses
3. **Authentication** is required for Order, Delivery, and Notification hubs
4. **MenuHub** is public so customers can receive updates without login
5. **No REST APIs** - all functionality is handled through Razor Pages
