using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Models;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPreparationService.Business.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;
    private readonly IVnPayService _vnPayService;

    public OrderService(
        IUnitOfWork unitOfWork, 
        ILogger<OrderService> logger,
        IVnPayService vnPayService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _vnPayService = vnPayService;
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, string userId)
    {
        _logger.LogInformation("Creating order for user {UserId}", userId);

        // Get user's cart
        var cart = await _unitOfWork.Carts.GetAllQueryable()
            .Include(c => c.CartItems)
                .ThenInclude(ci => ci.MenuMeal)
            .FirstOrDefaultAsync(c => c.AccountId == userId);

        if (cart == null || !cart.CartItems.Any())
        {
            throw new InvalidOperationException("Cart is empty");
        }

        // Calculate total amount
        decimal totalAmount = cart.CartItems.Sum(ci => ci.MenuMeal.Price * ci.Quantity);
        
        _logger.LogInformation("Order calculation - Cart has {ItemCount} items", cart.CartItems.Count);
        foreach (var item in cart.CartItems)
        {
            _logger.LogInformation("  Item: MenuMealId={MenuMealId}, Price={Price}, Quantity={Quantity}", 
                item.MenuMealId, item.MenuMeal.Price, item.Quantity);
        }
        _logger.LogInformation("Total amount calculated: {TotalAmount}", totalAmount);

        // Determine initial status based on payment method
        int initialStatusId = dto.PaymentMethod == "VNPay" ? 1 : 1; // 1 = Pending

        // Create order
        var order = new Order
        {
            Id = Guid.NewGuid().ToString(),
            CustomerId = userId,
            StatusId = initialStatusId,
            Date = DateTime.UtcNow,
            Amount = totalAmount,
            PaymentMethod = dto.PaymentMethod,
            Address = dto.Address,
            PhoneNumber = dto.PhoneNumber,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Create order details from cart items
        foreach (var cartItem in cart.CartItems)
        {
            var orderDetail = new OrderDetail
            {
                Id = Guid.NewGuid().ToString(),
                OrderId = order.Id,
                MenuMealId = cartItem.MenuMealId,
                Quantity = cartItem.Quantity,
                UnitPrice = cartItem.MenuMeal.Price,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            order.OrderDetails.Add(orderDetail);
        }

        await _unitOfWork.Orders.AddAsync(order);

        // Clear cart after order creation
        cart.CartItems.Clear();
        cart.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} created successfully", order.Id);

        return await GetOrderByIdAsync(order.Id);
    }

    public async Task<OrderDto> GetOrderByIdAsync(string orderId)
    {
        _logger.LogInformation("Getting order {OrderId}", orderId);

        var order = await _unitOfWork.Orders.GetAllQueryable()
            .Include(o => o.Status)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuMeal)
                    .ThenInclude(mm => mm.MenuMealRecipes)
                        .ThenInclude(mmr => mmr.Recipe)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order {orderId} not found");
        }

        return MapToOrderDto(order);
    }

    public async Task<OrderDto> GetOrderByNumberAsync(string orderNumber)
    {
        _logger.LogInformation("Getting order by number {OrderNumber}", orderNumber);

        var order = await _unitOfWork.Orders.GetAllQueryable()
            .Include(o => o.Status)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuMeal)
            .FirstOrDefaultAsync(o => o.Id == orderNumber);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order {orderNumber} not found");
        }

        return MapToOrderDto(order);
    }

    public async Task<List<OrderDto>> GetUserOrdersAsync(string userId)
    {
        _logger.LogInformation("Getting orders for user {UserId}", userId);

        var orders = await _unitOfWork.Orders.GetAllQueryable()
            .Where(o => o.CustomerId == userId)
            .Include(o => o.Status)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuMeal)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToOrderDto).ToList();
    }

    public async Task<PaginatedResult<OrderDto>> GetUserOrdersPaginatedAsync(string userId, PaginationParameters pagination)
    {
        _logger.LogInformation("Getting paginated orders for user {UserId}", userId);

        var query = _unitOfWork.Orders.GetAllQueryable()
            .Where(o => o.CustomerId == userId)
            .Include(o => o.Status)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuMeal)
            .OrderByDescending(o => o.CreatedAt);

        var totalCount = await query.CountAsync();
        var orders = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PaginatedResult<OrderDto>
        {
            Items = orders.Select(MapToOrderDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<List<OrderDto>> GetPendingOrdersAsync()
    {
        _logger.LogInformation("Getting pending orders");

        var orders = await _unitOfWork.Orders.GetAllQueryable()
            .Where(o => o.StatusId == 1) // Pending
            .Include(o => o.Status)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuMeal)
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToOrderDto).ToList();
    }

    public async Task<PaginatedResult<OrderDto>> GetPendingOrdersPaginatedAsync(PaginationParameters pagination)
    {
        _logger.LogInformation("Getting paginated pending orders");

        var query = _unitOfWork.Orders.GetAllQueryable()
            .Where(o => o.StatusId == 1) // Pending
            .Include(o => o.Status)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuMeal)
            .OrderBy(o => o.CreatedAt);

        var totalCount = await query.CountAsync();
        var orders = await query
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return new PaginatedResult<OrderDto>
        {
            Items = orders.Select(MapToOrderDto).ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageNumber,
            PageSize = pagination.PageSize
        };
    }

    public async Task<List<OrderDto>> GetAllOrdersAsync()
    {
        _logger.LogInformation("Getting all orders");

        var orders = await _unitOfWork.Orders.GetAllQueryable()
            .Include(o => o.Status)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.MenuMeal)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToOrderDto).ToList();
    }


    public async Task<OrderDto> ConfirmOrderAsync(string orderId, string staffId)
    {
        _logger.LogInformation("Confirming order {OrderId} by staff {StaffId}", orderId, staffId);

        var order = await _unitOfWork.Orders.GetAllQueryable()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order {orderId} not found");
        }

        if (order.StatusId != 1) // Not pending (1 = OrderPending)
        {
            throw new InvalidOperationException("Only pending orders can be confirmed");
        }

        order.StatusId = 3; // OrderConfirmed
        order.OrderConfirmedBy = staffId;
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return await GetOrderByIdAsync(orderId);
    }

    public async Task<OrderDto> CancelOrderAsync(string orderId, string reason, string staffId)
    {
        _logger.LogInformation("Cancelling order {OrderId} by staff {StaffId}", orderId, staffId);

        var order = await _unitOfWork.Orders.GetAllQueryable()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order {orderId} not found");
        }

        order.StatusId = 2; // OrderCanceled
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return await GetOrderByIdAsync(orderId);
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(string orderId, int statusId, string staffId)
    {
        _logger.LogInformation("Updating order {OrderId} status to {StatusId} by staff {StaffId}", orderId, statusId, staffId);

        var order = await _unitOfWork.Orders.GetAllQueryable()
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order {orderId} not found");
        }

        // Validate status exists
        var status = await _unitOfWork.Statuses.GetByIdAsync(statusId);
            
        if (status == null)
        {
            throw new InvalidOperationException($"Invalid status ID: {statusId}");
        }

        order.StatusId = statusId;
        order.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        return await GetOrderByIdAsync(orderId);
    }

    public async Task<decimal> CalculateDeliveryFeeAsync(string address)
    {
        // Simple flat rate for now
        await Task.CompletedTask;
        return 5.00m;
    }

    public async Task<bool> ValidateDeliveryDistanceAsync(string address)
    {
        // Accept all addresses for now
        await Task.CompletedTask;
        return true;
    }

    private OrderDto MapToOrderDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.Id.Substring(0, 8).ToUpper(),
            Status = order.Status?.Name ?? "Unknown",
            SubTotal = order.Amount,
            DeliveryFee = 0,
            TotalAmount = order.Amount,
            PaymentMethod = order.PaymentMethod,
            Address = order.Address,
            PhoneNumber = order.PhoneNumber,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            ConfirmedAt = order.OrderConfirmedBy != null ? order.UpdatedAt : null,
            Items = order.OrderDetails.Select(od => new OrderItemDetailDto
            {
                IngredientId = od.MenuMealId,
                IngredientName = GetMealName(od.MenuMeal),
                IngredientCategory = GetMealTypeName(od.MenuMeal?.MealTypeId ?? 0),
                Quantity = od.Quantity,
                Unit = "meal",
                UnitPrice = od.UnitPrice,
                TotalPrice = od.UnitPrice * od.Quantity
            }).ToList()
        };
    }

    private string GetMealName(MenuMeal? menuMeal)
    {
        if (menuMeal == null) return "Unknown Meal";
        
        var recipeNames = menuMeal.MenuMealRecipes
            .Select(mmr => mmr.Recipe?.RecipeName ?? "Unknown Recipe")
            .ToList();
        
        if (recipeNames.Count == 0)
            return $"{GetMealTypeName(menuMeal.MealTypeId)} Meal";
        
        return string.Join(" + ", recipeNames);
    }

    private string GetMealTypeName(int mealTypeId)
    {
        return mealTypeId switch
        {
            1 => "Breakfast",
            2 => "Lunch",
            3 => "Dinner",
            _ => "Unknown"
        };
    }
}
