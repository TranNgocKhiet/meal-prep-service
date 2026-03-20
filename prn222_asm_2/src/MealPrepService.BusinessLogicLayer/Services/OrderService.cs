using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class OrderService : IOrderService
    {
        private static class OrderStatuses
        {
            public const string Pending = "pending";
            public const string AwaitingOnlinePayment = "awaiting_online_payment";
            public const string PaymentFailed = "payment_failed";
            public const string PendingConfirmation = "pending_confirmation";
            public const string Confirmed = "confirmed";
            public const string Preparing = "preparing";
            public const string PreparingFailed = "preparing_failed";
            public const string Prepared = "prepared";
            public const string Cancelled = "cancelled";
            public const string Delivering = "delivering";
            public const string CustomerReceived = "customer_received";
            public const string CustomerReject = "customer_reject";
            public const string Failed = "failed";
        }

        private readonly IUnitOfWork _unitOfWork;
        private readonly IVnpayService _vnpayService;
        private readonly IDeliveryService _deliveryService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            IUnitOfWork unitOfWork, 
            IVnpayService vnpayService,
            IDeliveryService deliveryService,
            ILogger<OrderService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _vnpayService = vnpayService ?? throw new ArgumentNullException(nameof(vnpayService));
            _deliveryService = deliveryService ?? throw new ArgumentNullException(nameof(deliveryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<OrderDto> CreateOrderAsync(Guid accountId, List<OrderItemDto> items)
        {
            if (items == null || !items.Any())
            {
                throw new BusinessException("Order must contain at least one item");
            }

            // Validate account exists
            var account = await _unitOfWork.Accounts.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new BusinessException($"Account with ID {accountId} not found");
            }

            var createdOrder = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Create order entity
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatuses.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                decimal totalAmount = 0;
                var orderDetails = new List<OrderDetail>();

                // Process each order item
                foreach (var item in items)
                {
                    if (item.Quantity <= 0)
                    {
                        throw new BusinessException("Order item quantity must be greater than zero");
                    }

                    // Get menu meal and validate availability
                    var menuMeal = await _unitOfWork.MenuMeals.GetByIdAsync(item.MenuMealId);
                    if (menuMeal == null)
                    {
                        throw new BusinessException($"Menu meal with ID {item.MenuMealId} not found");
                    }

                    if (menuMeal.AvailableQuantity < item.Quantity)
                    {
                        throw new BusinessException($"Insufficient quantity available for menu meal {item.MenuMealId}. Available: {menuMeal.AvailableQuantity}, Requested: {item.Quantity}");
                    }

                    // Create order detail
                    var orderDetail = new OrderDetail
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        MenuMealId = item.MenuMealId,
                        Quantity = item.Quantity,
                        UnitPrice = menuMeal.Price,
                        CreatedAt = DateTime.UtcNow
                    };

                    orderDetails.Add(orderDetail);
                    totalAmount += menuMeal.Price * item.Quantity;
                }

                order.TotalAmount = totalAmount;
                order.OrderDetails = orderDetails;

                await _unitOfWork.Orders.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} created for account {AccountId} with total amount {TotalAmount}", 
                    order.Id, accountId, totalAmount);

                return order;
            });

            if (createdOrder == null)
            {
                throw new BusinessException("Failed to retrieve created order");
            }

            return await MapToDtoAsync(createdOrder);
        }

        public async Task<OrderDto> ProcessPaymentAsync(Guid orderId, string paymentMethod, string? deliveryAddress = null, DateTime? preferredDeliveryTime = null, string? customerPhone = null)
        {
            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                throw new BusinessException("Payment method is required");
            }

            var normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();
            var validPaymentMethods = new[] { "COD", "VNPAY" };
            if (!validPaymentMethods.Contains(normalizedPaymentMethod))
            {
                throw new BusinessException($"Invalid payment method: {paymentMethod}. Valid methods are: {string.Join(", ", validPaymentMethods)}");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {orderId} not found");
            }

            if (order.Status != OrderStatuses.Pending && order.Status != OrderStatuses.PaymentFailed)
            {
                throw new BusinessException($"Order {orderId} cannot be paid. Current status: {order.Status}");
            }

            if (string.IsNullOrWhiteSpace(deliveryAddress))
            {
                throw new BusinessException("Delivery address is required");
            }

            if (string.IsNullOrWhiteSpace(customerPhone))
            {
                throw new BusinessException("Customer phone number is required for delivery");
            }

            var normalizedPhone = customerPhone.Trim();
            if (normalizedPhone.Length < 8 || normalizedPhone.Length > 20)
            {
                throw new BusinessException("Customer phone number must be between 8 and 20 characters");
            }

            var deliveryTime = preferredDeliveryTime ?? DateTime.Now.AddDays(1).Date.AddHours(18);
            if (deliveryTime <= DateTime.Now)
            {
                throw new BusinessException("Preferred delivery time must be in the future");
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                order.PaymentMethod = normalizedPaymentMethod;
                order.UpdatedAt = DateTime.UtcNow;
                order.PaymentConfirmedAt = null;
                order.PaymentConfirmedBy = null;

                await UpsertDeliveryScheduleAsync(orderId, deliveryAddress.Trim(), deliveryTime, normalizedPhone);

                if (normalizedPaymentMethod == "COD")
                {
                    order.Status = OrderStatuses.PendingConfirmation;
                    _logger.LogInformation("COD order {OrderId} moved to pending confirmation", orderId);
                }
                else
                {
                    order.Status = OrderStatuses.AwaitingOnlinePayment;
                    _logger.LogInformation("VNPAY order {OrderId} awaiting payment callback", orderId);
                }

                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();

                return;
            });

            return await MapToDtoAsync(order);
        }

        public async Task<OrderDto> GetByIdAsync(Guid orderId)
        {
            var order = await _unitOfWork.Orders.GetWithDetailsAsync(orderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {orderId} not found");
            }

            return await MapToDtoAsync(order);
        }

        public async Task<IEnumerable<OrderDto>> GetByAccountIdAsync(Guid accountId)
        {
            var orders = await _unitOfWork.Orders.GetByAccountIdAsync(accountId);
            var orderDtos = new List<OrderDto>();

            foreach (var order in orders)
            {
                orderDtos.Add(await MapToDtoAsync(order));
            }

            return orderDtos;
        }

        public async Task<OrderDto> ProcessVnpayCallbackAsync(VnpayCallbackDto callbackDto)
        {
            var callbackResult = await _vnpayService.ProcessCallbackAsync(callbackDto);
            
            if (!callbackResult.IsSuccess)
            {
                throw new BusinessException($"Invalid VNPAY callback: {callbackResult.Message}");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(callbackResult.OrderId);
            
            if (order == null)
            {
                throw new BusinessException("Order not found");
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (callbackResult.ResponseCode == "00") // Success
                {
                    order.Status = OrderStatuses.Confirmed;
                    order.VnpayTransactionId = callbackResult.TransactionId;
                    order.PaymentConfirmedAt = DateTime.UtcNow;
                    order.UpdatedAt = DateTime.UtcNow;

                    _logger.LogInformation("VNPAY payment successful for order {OrderId}, transaction {TransactionId}. Status set to {Status}", 
                        order.Id, callbackResult.TransactionId, order.Status);
                }
                else
                {
                    order.Status = OrderStatuses.PaymentFailed;
                    order.UpdatedAt = DateTime.UtcNow;

                    var existingSchedule = await _unitOfWork.DeliverySchedules.FindAsync(d => d.OrderId == order.Id);
                    foreach (var schedule in existingSchedule)
                    {
                        await _unitOfWork.DeliverySchedules.DeleteAsync(schedule.Id);
                    }
                    
                    _logger.LogWarning("VNPAY payment failed for order {OrderId}, response code {ResponseCode}: {Message}", 
                        order.Id, callbackResult.ResponseCode, callbackResult.Message);
                }
                
                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();

                return;
            });

            return await MapToDtoAsync(order);
        }

        public async Task<OrderDto> ConfirmCashPaymentAsync(Guid orderId, Guid deliveryManId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            
            if (order == null)
            {
                throw new BusinessException("Order not found");
            }
            
            if (order.PaymentMethod != "COD")
            {
                throw new BusinessException("Order is not a Cash on Delivery order");
            }
            
            if (order.PaymentConfirmedAt.HasValue)
            {
                throw new BusinessException("Cash payment is already confirmed for this order");
            }
            
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                order.PaymentConfirmedAt = DateTime.UtcNow;
                order.PaymentConfirmedBy = deliveryManId;
                order.UpdatedAt = DateTime.UtcNow;
                
                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();

                return;
            });

            _logger.LogInformation("Cash payment confirmed for COD order {OrderId} by delivery man {DeliveryManId}", 
                orderId, deliveryManId);

            return await MapToDtoAsync(order);
        }

        public async Task UpdateOrderStatusAsync(Guid orderId, string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                throw new BusinessException("Status is required");
            }

            var normalizedStatus = status.Trim().ToLowerInvariant();
            var validStatuses = new[]
            {
                OrderStatuses.Pending,
                OrderStatuses.AwaitingOnlinePayment,
                OrderStatuses.PaymentFailed,
                OrderStatuses.PendingConfirmation,
                OrderStatuses.Confirmed,
                OrderStatuses.Preparing,
                OrderStatuses.PreparingFailed,
                OrderStatuses.Prepared,
                OrderStatuses.Cancelled,
                OrderStatuses.Delivering,
                OrderStatuses.CustomerReceived,
                OrderStatuses.CustomerReject,
                OrderStatuses.Failed
            };

            if (!validStatuses.Contains(normalizedStatus))
            {
                throw new BusinessException($"Invalid status: {status}. Valid statuses are: {string.Join(", ", validStatuses)}");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {orderId} not found");
            }

            order.Status = normalizedStatus;
            order.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, normalizedStatus);
        }

        public async Task<IEnumerable<OrderDto>> GetOperationsOrdersAsync()
        {
            var operationStatuses = new[]
            {
                OrderStatuses.Pending,
                OrderStatuses.PendingConfirmation,
                OrderStatuses.Confirmed,
                OrderStatuses.Preparing,
                OrderStatuses.PreparingFailed,
                OrderStatuses.Prepared,
                OrderStatuses.Delivering,
                OrderStatuses.Cancelled
            };

            var orders = await _unitOfWork.Orders.GetAllAsync();
            var operationOrders = orders
                .Where(o => operationStatuses.Contains(o.Status, StringComparer.OrdinalIgnoreCase))
                .OrderByDescending(o => o.UpdatedAt ?? o.OrderDate)
                .ToList();

            var result = new List<OrderDto>();
            foreach (var order in operationOrders)
            {
                result.Add(await GetByIdAsync(order.Id));
            }

            return result;
        }

        public async Task<OrderDto> TransitionOrderForOperationsAsync(Guid orderId, string targetStatus, Guid staffId)
        {
            if (string.IsNullOrWhiteSpace(targetStatus))
            {
                throw new BusinessException("Target status is required");
            }

            var normalizedTarget = targetStatus.Trim().ToLowerInvariant();
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {orderId} not found");
            }

            var currentStatus = order.Status.Trim().ToLowerInvariant();
            var allowedTransitions = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [OrderStatuses.Pending] = new[] { OrderStatuses.Confirmed, OrderStatuses.Cancelled },
                [OrderStatuses.PendingConfirmation] = new[] { OrderStatuses.Confirmed, OrderStatuses.Cancelled },
                [OrderStatuses.Confirmed] = new[] { OrderStatuses.Pending, OrderStatuses.Preparing, OrderStatuses.Cancelled },
                [OrderStatuses.Cancelled] = new[] { OrderStatuses.Pending },
                [OrderStatuses.Preparing] = new[] { OrderStatuses.Confirmed, OrderStatuses.Cancelled, OrderStatuses.Prepared, OrderStatuses.PreparingFailed },
                [OrderStatuses.PreparingFailed] = new[] { OrderStatuses.Prepared, OrderStatuses.Preparing },
                [OrderStatuses.Prepared] = new[] { OrderStatuses.Preparing, OrderStatuses.Delivering, OrderStatuses.PreparingFailed },
                [OrderStatuses.Delivering] = new[] { OrderStatuses.Prepared }
            };

            if (!allowedTransitions.TryGetValue(currentStatus, out var allowedTargets) ||
                !allowedTargets.Contains(normalizedTarget, StringComparer.OrdinalIgnoreCase))
            {
                throw new BusinessException($"Invalid order transition: {currentStatus} -> {normalizedTarget}");
            }

            if (currentStatus == OrderStatuses.Confirmed && normalizedTarget == OrderStatuses.Preparing)
            {
                await DeductMenuMealQuantitiesForOrderAsync(orderId);
            }

            order.Status = normalizedTarget;
            order.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Staff {StaffId} transitioned order {OrderId} from {FromStatus} to {ToStatus}",
                staffId, orderId, currentStatus, normalizedTarget);

            return await GetByIdAsync(orderId);
        }

        private async Task DeductMenuMealQuantitiesForOrderAsync(Guid orderId)
        {
            var orderDetails = await _unitOfWork.OrderDetails.FindAsync(od => od.OrderId == orderId);
            var details = orderDetails.ToList();

            if (!details.Any())
            {
                throw new BusinessException($"Order {orderId} has no order details to prepare");
            }

            foreach (var detail in details)
            {
                var menuMeal = await _unitOfWork.MenuMeals.GetByIdAsync(detail.MenuMealId);
                if (menuMeal == null)
                {
                    throw new BusinessException($"Menu meal with ID {detail.MenuMealId} not found");
                }

                if (menuMeal.AvailableQuantity < detail.Quantity)
                {
                    throw new BusinessException($"Insufficient quantity available for menu meal {detail.MenuMealId}. Available: {menuMeal.AvailableQuantity}, Required: {detail.Quantity}");
                }

                menuMeal.AvailableQuantity -= detail.Quantity;
                menuMeal.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.MenuMeals.UpdateAsync(menuMeal);
            }
        }

        public async Task<OrderDto> ConfirmOrderByStaffAsync(Guid orderId, bool isConfirmed, Guid staffId)
        {
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {orderId} not found");
            }

            if (order.Status != OrderStatuses.PendingConfirmation)
            {
                throw new BusinessException($"Only pending confirmation orders can be reviewed. Current status: {order.Status}");
            }

            order.Status = isConfirmed ? OrderStatuses.Confirmed : OrderStatuses.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Staff {StaffId} updated order {OrderId} to {Status}", staffId, orderId, order.Status);

            return await GetByIdAsync(orderId);
        }

        public async Task<IEnumerable<OrderDto>> GetPendingConfirmationOrdersAsync()
        {
            var orders = await _unitOfWork.Orders.GetAllAsync();
            var pending = orders
                .Where(x => x.Status == OrderStatuses.PendingConfirmation)
                .OrderBy(x => x.OrderDate)
                .ToList();

            var result = new List<OrderDto>();
            foreach (var order in pending)
            {
                result.Add(await GetByIdAsync(order.Id));
            }

            return result;
        }

        private async Task<bool> ProcessPaymentWithGateway(decimal amount, string paymentMethod)
        {
            // Placeholder for payment gateway integration
            // In a real implementation, this would integrate with a payment processor
            await Task.Delay(100); // Simulate network call
            
            // For demo purposes, assume payment succeeds 90% of the time
            var random = new Random();
            return random.NextDouble() > 0.1;
        }

        private async Task<OrderDto> MapToDtoAsync(Order order)
        {
            var orderDetails = new List<OrderDetailDto>();

            if (order.OrderDetails != null)
            {
                foreach (var detail in order.OrderDetails)
                {
                    var menuMeal = detail.MenuMeal ?? await _unitOfWork.MenuMeals.GetByIdAsync(detail.MenuMealId);
                    
                    var orderDetailDto = new OrderDetailDto
                    {
                        Id = detail.Id,
                        OrderId = detail.OrderId,
                        MenuMealId = detail.MenuMealId,
                        Quantity = detail.Quantity,
                        UnitPrice = detail.UnitPrice
                    };

                    if (menuMeal != null)
                    {
                        orderDetailDto.MenuMeal = new MenuMealDto
                        {
                            Id = menuMeal.Id,
                            MenuId = menuMeal.MenuId,
                            RecipeId = menuMeal.RecipeId,
                            RecipeName = menuMeal.Recipe?.RecipeName ?? string.Empty,
                            Price = menuMeal.Price,
                            AvailableQuantity = menuMeal.AvailableQuantity,
                            IsSoldOut = menuMeal.AvailableQuantity == 0
                        };
                    }

                    orderDetails.Add(orderDetailDto);
                }
            }

            DeliveryScheduleDto? deliveryScheduleDto = null;
            if (order.DeliverySchedule != null)
            {
                deliveryScheduleDto = new DeliveryScheduleDto
                {
                    Id = order.DeliverySchedule.Id,
                    OrderId = order.DeliverySchedule.OrderId,
                    DeliveryTime = order.DeliverySchedule.DeliveryTime,
                    Address = order.DeliverySchedule.Address,
                    CustomerPhone = order.DeliverySchedule.CustomerPhone,
                    DriverContact = order.DeliverySchedule.DriverContact
                };
            }

            var account = order.Account ?? await _unitOfWork.Accounts.GetByIdAsync(order.AccountId);

            return new OrderDto
            {
                Id = order.Id,
                AccountId = order.AccountId,
                OrderDate = order.OrderDate,
                UpdatedAt = order.UpdatedAt,
                TotalAmount = order.TotalAmount,
                PaymentMethod = order.PaymentMethod,
                Status = order.Status,
                VnpayTransactionId = order.VnpayTransactionId,
                PaymentConfirmedAt = order.PaymentConfirmedAt,
                PaymentConfirmedBy = order.PaymentConfirmedBy,
                OrderDetails = orderDetails,
                DeliverySchedule = deliveryScheduleDto,
                CustomerName = account?.FullName ?? string.Empty,
                CustomerContact = deliveryScheduleDto?.CustomerPhone ?? string.Empty,
                DeliveryAddress = deliveryScheduleDto?.Address ?? string.Empty
            };
        }

        private async Task UpsertDeliveryScheduleAsync(Guid orderId, string deliveryAddress, DateTime deliveryTime, string customerPhone)
        {
            var existing = await _unitOfWork.DeliverySchedules.FindAsync(d => d.OrderId == orderId);
            var schedule = existing.FirstOrDefault();

            if (schedule == null)
            {
                schedule = new DeliverySchedule
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    Address = deliveryAddress,
                    CustomerPhone = customerPhone,
                    DeliveryTime = deliveryTime,
                    DriverContact = "Unassigned",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.DeliverySchedules.AddAsync(schedule);
            }
            else
            {
                schedule.Address = deliveryAddress;
                schedule.CustomerPhone = customerPhone;
                schedule.DeliveryTime = deliveryTime;
                schedule.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.DeliverySchedules.UpdateAsync(schedule);
            }
        }
    }
}