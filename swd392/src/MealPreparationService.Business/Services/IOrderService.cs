using MealPreparationService.Business.DTOs;
using MealPreparationService.Business.Models;

namespace MealPreparationService.Business.Services;

/// <summary>
/// Service interface for order management operations
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Creates a new order with items, delivery address, and payment method
    /// </summary>
    Task<OrderDto> CreateOrderAsync(CreateOrderDto dto, string userId);

    /// <summary>
    /// Gets an order by its ID
    /// </summary>
    Task<OrderDto> GetOrderByIdAsync(string orderId);

    /// <summary>
    /// Gets an order by its order number
    /// </summary>
    Task<OrderDto> GetOrderByNumberAsync(string orderNumber);

    /// <summary>
    /// Gets all orders for a specific user
    /// </summary>
    Task<List<OrderDto>> GetUserOrdersAsync(string userId);

    /// <summary>
    /// Gets paginated orders for a specific user
    /// </summary>
    Task<PaginatedResult<OrderDto>> GetUserOrdersPaginatedAsync(string userId, PaginationParameters pagination);

    /// <summary>
    /// Gets all pending orders for staff to review
    /// </summary>
    Task<List<OrderDto>> GetPendingOrdersAsync();

    /// <summary>
    /// Gets paginated pending orders for staff to review
    /// </summary>
    Task<PaginatedResult<OrderDto>> GetPendingOrdersPaginatedAsync(PaginationParameters pagination);

    /// <summary>
    /// Gets all orders for staff (all statuses)
    /// </summary>
    Task<List<OrderDto>> GetAllOrdersAsync();

    /// <summary>
    /// Confirms an order (staff only)
    /// </summary>
    Task<OrderDto> ConfirmOrderAsync(string orderId, string staffId);

    /// <summary>
    /// Cancels an order with a reason (staff only)
    /// </summary>
    Task<OrderDto> CancelOrderAsync(string orderId, string reason, string staffId);

    /// <summary>
    /// Updates order status (staff only)
    /// </summary>
    Task<OrderDto> UpdateOrderStatusAsync(string orderId, int statusId, string staffId);

    /// <summary>
    /// Calculates delivery fee for an address
    /// </summary>
    Task<decimal> CalculateDeliveryFeeAsync(string address);

    /// <summary>
    /// Validates if delivery address is within service area
    /// </summary>
    Task<bool> ValidateDeliveryDistanceAsync(string address);
}
