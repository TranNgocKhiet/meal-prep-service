using MealPrepService.BusinessLogicLayer.DTOs;

namespace MealPrepService.BusinessLogicLayer.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(Guid accountId, List<OrderItemDto> items);
    Task<OrderDto> ProcessPaymentAsync(Guid orderId, string paymentMethod, string? deliveryAddress = null, DateTime? preferredDeliveryTime = null, string? customerPhone = null);
    Task<OrderDto> ProcessVnpayCallbackAsync(VnpayCallbackDto callbackDto);
    Task<OrderDto> ConfirmCashPaymentAsync(Guid orderId, Guid deliveryManId);
    Task<OrderDto> ConfirmOrderByStaffAsync(Guid orderId, bool isConfirmed, Guid staffId);
    Task<IEnumerable<OrderDto>> GetPendingConfirmationOrdersAsync();
    Task<OrderDto> GetByIdAsync(Guid orderId);
    Task<IEnumerable<OrderDto>> GetByAccountIdAsync(Guid accountId);
    Task UpdateOrderStatusAsync(Guid orderId, string status);
    Task<IEnumerable<OrderDto>> GetOperationsOrdersAsync();
    Task<OrderDto> TransitionOrderForOperationsAsync(Guid orderId, string targetStatus, Guid staffId);
}