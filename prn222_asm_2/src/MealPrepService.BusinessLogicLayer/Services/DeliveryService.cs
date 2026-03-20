using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;
using Microsoft.Extensions.Logging;

namespace MealPrepService.BusinessLogicLayer.Services
{
    public class DeliveryService : IDeliveryService
    {
        private static class OrderStatuses
        {
            public const string PendingConfirmation = "pending_confirmation";
            public const string Confirmed = "confirmed";
            public const string Prepared = "prepared";
            public const string Delivering = "delivering";
            public const string CustomerReceived = "customer_received";
            public const string CustomerReject = "customer_reject";
            public const string Failed = "failed";
            public const string Cancelled = "cancelled";
        }

        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeliveryService> _logger;

        public DeliveryService(IUnitOfWork unitOfWork, ILogger<DeliveryService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DeliveryScheduleDto> CreateDeliveryScheduleAsync(Guid orderId, DeliveryScheduleDto dto)
        {
            if (dto == null)
            {
                throw new ArgumentNullException(nameof(dto));
            }

            // Validate order exists
            var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {orderId} not found");
            }

            // Check if delivery schedule already exists for this order
            var existingDelivery = await _unitOfWork.DeliverySchedules.FindAsync(d => d.OrderId == orderId);
            if (existingDelivery.Any())
            {
                throw new BusinessException($"Delivery schedule already exists for order {orderId}");
            }

            // Validate delivery time is in the future
            if (dto.DeliveryTime <= DateTime.UtcNow)
            {
                throw new BusinessException("Delivery time must be in the future");
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(dto.Address))
            {
                throw new BusinessException("Delivery address is required");
            }

            if (dto.DeliveryManId.HasValue)
            {
                var assigned = await _unitOfWork.Accounts.GetByIdAsync(dto.DeliveryManId.Value);
                if (assigned == null || !string.Equals(assigned.Role, "DeliveryMan", StringComparison.OrdinalIgnoreCase))
                {
                    throw new BusinessException("Assigned delivery account is invalid");
                }
            }

            // Create delivery schedule entity
            var deliverySchedule = new DeliverySchedule
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                DeliveryManId = dto.DeliveryManId,
                DeliveryTime = dto.DeliveryTime,
                Address = dto.Address.Trim(),
                CustomerPhone = dto.CustomerPhone?.Trim() ?? string.Empty,
                DriverContact = dto.DriverContact?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.DeliverySchedules.AddAsync(deliverySchedule);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Delivery schedule created for order {OrderId} with delivery time {DeliveryTime}", 
                orderId, dto.DeliveryTime);

            return MapToDto(deliverySchedule);
        }

        public async Task<IEnumerable<DeliveryScheduleDto>> GetByAccountIdAsync(Guid accountId)
        {
            // Get all orders for the account
            var orders = await _unitOfWork.Orders.GetByAccountIdAsync(accountId);
            var orderIds = orders.Select(o => o.Id).ToList();

            if (!orderIds.Any())
            {
                return new List<DeliveryScheduleDto>();
            }

            // Get delivery schedules for these orders
            var deliverySchedules = await _unitOfWork.DeliverySchedules.FindAsync(d => orderIds.Contains(d.OrderId));
            
            var deliveryDtos = new List<DeliveryScheduleDto>();
            foreach (var delivery in deliverySchedules)
            {
                var order = orders.FirstOrDefault(o => o.Id == delivery.OrderId);
                var dto = MapToDto(delivery);
                if (delivery.DeliveryManId.HasValue)
                {
                    var assigned = await _unitOfWork.Accounts.GetByIdAsync(delivery.DeliveryManId.Value);
                    dto.DeliveryManName = assigned?.FullName ?? string.Empty;
                }
                
                if (order != null)
                {
                    var customer = await _unitOfWork.Accounts.GetByIdAsync(order.AccountId);
                    dto.Order = new OrderDto
                    {
                        Id = order.Id,
                        AccountId = order.AccountId,
                        OrderDate = order.OrderDate,
                        TotalAmount = order.TotalAmount,
                        PaymentMethod = order.PaymentMethod,
                        Status = order.Status,
                        CustomerName = ResolveAccountDisplayName(customer),
                        CustomerContact = dto.CustomerContact,
                        DeliveryAddress = dto.Address,
                        OrderDetails = await BuildOrderDetailsAsync(order.Id)
                    };
                }
                
                deliveryDtos.Add(dto);
            }

            return deliveryDtos.OrderBy(d => d.DeliveryTime);
        }

        public async Task<IEnumerable<DeliveryScheduleDto>> GetByDeliveryManAsync(Guid deliveryManId)
        {
            // Validate delivery man exists and has correct role
            var deliveryMan = await _unitOfWork.Accounts.GetByIdAsync(deliveryManId);
            if (deliveryMan == null)
            {
                throw new BusinessException($"Account with ID {deliveryManId} not found");
            }

            if (deliveryMan.Role != "DeliveryMan")
            {
                throw new BusinessException($"Account {deliveryManId} is not a delivery man. Current role: {deliveryMan.Role}");
            }

            var allDeliveries = await _unitOfWork.DeliverySchedules.GetAllAsync();
            
            var deliveryDtos = new List<DeliveryScheduleDto>();
            foreach (var delivery in allDeliveries)
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(delivery.OrderId);
                var dto = MapToDto(delivery);
                if (delivery.DeliveryManId.HasValue)
                {
                    var assigned = await _unitOfWork.Accounts.GetByIdAsync(delivery.DeliveryManId.Value);
                    dto.DeliveryManName = assigned?.FullName ?? string.Empty;
                }
                
                if (order != null)
                {
                    var customer = await _unitOfWork.Accounts.GetByIdAsync(order.AccountId);
                    dto.Order = new OrderDto
                    {
                        Id = order.Id,
                        AccountId = order.AccountId,
                        OrderDate = order.OrderDate,
                        TotalAmount = order.TotalAmount,
                        PaymentMethod = order.PaymentMethod,
                        Status = order.Status,
                        CustomerName = ResolveAccountDisplayName(customer),
                        CustomerContact = dto.CustomerContact,
                        DeliveryAddress = dto.Address,
                        OrderDetails = await BuildOrderDetailsAsync(order.Id)
                    };
                }

                if (dto.Order == null)
                {
                    continue;
                }

                var isAssignedToCurrentDeliveryMan = delivery.DeliveryManId == deliveryManId;
                var isOwnerInProgress = dto.Order.Status == OrderStatuses.Delivering && isAssignedToCurrentDeliveryMan;
                var isOwnerFinal = (dto.Order.Status == OrderStatuses.CustomerReceived || dto.Order.Status == OrderStatuses.CustomerReject || dto.Order.Status == OrderStatuses.Failed) && isAssignedToCurrentDeliveryMan;

                if (isOwnerInProgress || isOwnerFinal)
                {
                    deliveryDtos.Add(dto);
                }
            }

            return deliveryDtos.OrderBy(d => d.DeliveryTime);
        }

        public async Task<IEnumerable<DeliveryScheduleDto>> GetAllForOperationsAsync()
        {
            var allDeliveries = await _unitOfWork.DeliverySchedules.GetAllAsync();
            var result = new List<DeliveryScheduleDto>();

            foreach (var delivery in allDeliveries)
            {
                var order = await _unitOfWork.Orders.GetByIdAsync(delivery.OrderId);
                if (order == null)
                {
                    continue;
                }

                var dto = MapToDto(delivery);

                if (delivery.DeliveryManId.HasValue)
                {
                    var assigned = await _unitOfWork.Accounts.GetByIdAsync(delivery.DeliveryManId.Value);
                    dto.DeliveryManName = assigned?.FullName ?? string.Empty;
                }

                var customer = await _unitOfWork.Accounts.GetByIdAsync(order.AccountId);
                dto.Order = new OrderDto
                {
                    Id = order.Id,
                    AccountId = order.AccountId,
                    OrderDate = order.OrderDate,
                    TotalAmount = order.TotalAmount,
                    PaymentMethod = order.PaymentMethod,
                    Status = order.Status,
                    CustomerName = ResolveAccountDisplayName(customer),
                    CustomerContact = dto.CustomerContact,
                    DeliveryAddress = dto.Address,
                    OrderDetails = await BuildOrderDetailsAsync(order.Id)
                };

                result.Add(dto);
            }

            return result.OrderByDescending(x => x.DeliveryTime);
        }

        public async Task AcceptDeliveryAsync(Guid deliveryId, Guid deliveryManId)
        {
            var deliveryMan = await _unitOfWork.Accounts.GetByIdAsync(deliveryManId);
            if (deliveryMan == null || deliveryMan.Role != "DeliveryMan")
            {
                throw new BusinessException("Only delivery man accounts can accept deliveries");
            }

            var deliverySchedule = await _unitOfWork.DeliverySchedules.GetByIdAsync(deliveryId);
            if (deliverySchedule == null)
            {
                throw new BusinessException($"Delivery schedule with ID {deliveryId} not found");
            }

            // Get the associated order
            var order = await _unitOfWork.Orders.GetByIdAsync(deliverySchedule.OrderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {deliverySchedule.OrderId} not found");
            }

            if (order.Status != OrderStatuses.Confirmed && order.Status != OrderStatuses.Delivering)
            {
                throw new BusinessException($"Only confirmed or scheduled orders can be accepted for delivery. Current status: {order.Status}");
            }

            if (deliverySchedule.DeliveryManId.HasValue && deliverySchedule.DeliveryManId != deliveryManId)
            {
                throw new BusinessException("This delivery is already assigned to another delivery man");
            }

            // Keep scheduled deliveries in delivering state and move confirmed ones into delivering.
            if (order.Status == OrderStatuses.Confirmed)
            {
                order.Status = OrderStatuses.Delivering;
            }
            order.UpdatedAt = DateTime.UtcNow;
            deliverySchedule.DeliveryManId = deliveryManId;
            deliverySchedule.DriverContact = deliveryMan.FullName;
            deliverySchedule.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Orders.UpdateAsync(order);
            await _unitOfWork.DeliverySchedules.UpdateAsync(deliverySchedule);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Delivery {DeliveryId} accepted by delivery man {DeliveryManId}", deliveryId, deliveryManId);
        }

        public async Task AssignDeliveryManAsync(Guid deliveryId, Guid deliveryManId)
        {
            var deliveryMan = await _unitOfWork.Accounts.GetByIdAsync(deliveryManId);
            if (deliveryMan == null || !string.Equals(deliveryMan.Role, "DeliveryMan", StringComparison.OrdinalIgnoreCase))
            {
                throw new BusinessException("Assigned account must be a valid delivery man");
            }

            var deliverySchedule = await _unitOfWork.DeliverySchedules.GetByIdAsync(deliveryId);
            if (deliverySchedule == null)
            {
                throw new BusinessException($"Delivery schedule with ID {deliveryId} not found");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(deliverySchedule.OrderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {deliverySchedule.OrderId} not found");
            }

            if (order.Status != OrderStatuses.Prepared && order.Status != OrderStatuses.Delivering)
            {
                throw new BusinessException($"Cannot assign delivery man while order is in status: {order.Status}");
            }

            deliverySchedule.DeliveryManId = deliveryManId;
            deliverySchedule.DriverContact = deliveryMan.FullName;
            deliverySchedule.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.DeliverySchedules.UpdateAsync(deliverySchedule);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Delivery schedule {DeliveryId} assigned to delivery man {DeliveryManId}", deliveryId, deliveryManId);
        }

        public async Task CompleteDeliveryAsync(Guid deliveryId, Guid deliveryManId, string resultStatus)
        {
            if (string.IsNullOrWhiteSpace(resultStatus))
            {
                throw new BusinessException("Delivery result status is required");
            }

            var normalized = resultStatus.Trim().ToLowerInvariant();
            var allowed = new[] { OrderStatuses.Delivering, OrderStatuses.CustomerReceived, OrderStatuses.CustomerReject, OrderStatuses.Failed };
            if (!allowed.Contains(normalized))
            {
                throw new BusinessException($"Invalid delivery result. Allowed values: {string.Join(", ", allowed)}");
            }

            var editableStatuses = new[]
            {
                OrderStatuses.Delivering,
                OrderStatuses.CustomerReceived,
                OrderStatuses.CustomerReject,
                OrderStatuses.Failed
            };

            var deliverySchedule = await _unitOfWork.DeliverySchedules.GetByIdAsync(deliveryId);
            if (deliverySchedule == null)
            {
                throw new BusinessException($"Delivery schedule with ID {deliveryId} not found");
            }

            var order = await _unitOfWork.Orders.GetByIdAsync(deliverySchedule.OrderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {deliverySchedule.OrderId} not found");
            }

            if (!deliverySchedule.DeliveryManId.HasValue || deliverySchedule.DeliveryManId != deliveryManId)
            {
                throw new BusinessException("You can only complete deliveries assigned to you");
            }

            if (!editableStatuses.Contains(order.Status))
            {
                throw new BusinessException($"Delivery result cannot be changed from current status: {order.Status}");
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                order.Status = normalized;
                order.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Orders.UpdateAsync(order);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Delivery {DeliveryId} completed with status {Status} for order {OrderId}", 
                    deliveryId, normalized, deliverySchedule.OrderId);
            });
        }

        public async Task UpdateDeliveryTimeAsync(Guid deliveryId, DateTime newTime)
        {
            // Validate delivery time is in the future
            if (newTime <= DateTime.UtcNow)
            {
                throw new BusinessException("Delivery time must be in the future");
            }

            var deliverySchedule = await _unitOfWork.DeliverySchedules.GetByIdAsync(deliveryId);
            if (deliverySchedule == null)
            {
                throw new BusinessException($"Delivery schedule with ID {deliveryId} not found");
            }

            // Check if the associated order is still in a state that allows delivery time updates
            var order = await _unitOfWork.Orders.GetByIdAsync(deliverySchedule.OrderId);
            if (order == null)
            {
                throw new BusinessException($"Order with ID {deliverySchedule.OrderId} not found");
            }

            if (order.Status == OrderStatuses.CustomerReceived || order.Status == OrderStatuses.CustomerReject || order.Status == OrderStatuses.Failed || order.Status == OrderStatuses.Cancelled)
            {
                throw new BusinessException($"Cannot update delivery time for order {deliverySchedule.OrderId} - order is already delivered");
            }

            deliverySchedule.DeliveryTime = newTime;
            deliverySchedule.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.DeliverySchedules.UpdateAsync(deliverySchedule);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Delivery time updated for delivery {DeliveryId} to {NewTime}", 
                deliveryId, newTime);
        }

        private async Task<List<OrderDetailDto>> BuildOrderDetailsAsync(Guid orderId)
        {
            var orderDetails = await _unitOfWork.OrderDetails.FindAsync(od => od.OrderId == orderId);
            var result = new List<OrderDetailDto>();

            foreach (var detail in orderDetails)
            {
                MenuMealDto? menuMealDto = null;
                var menuMeal = await _unitOfWork.MenuMeals.GetByIdAsync(detail.MenuMealId);
                if (menuMeal != null)
                {
                    var recipeName = string.Empty;
                    var recipe = await _unitOfWork.Recipes.GetByIdAsync(menuMeal.RecipeId);
                    if (recipe != null)
                    {
                        recipeName = recipe.RecipeName;
                    }

                    menuMealDto = new MenuMealDto
                    {
                        Id = menuMeal.Id,
                        MenuId = menuMeal.MenuId,
                        RecipeId = menuMeal.RecipeId,
                        RecipeName = recipeName,
                        Price = menuMeal.Price,
                        AvailableQuantity = menuMeal.AvailableQuantity,
                        IsSoldOut = menuMeal.AvailableQuantity == 0
                    };
                }

                result.Add(new OrderDetailDto
                {
                    Id = detail.Id,
                    OrderId = detail.OrderId,
                    MenuMealId = detail.MenuMealId,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    MenuMeal = menuMealDto
                });
            }

            return result;
        }

        private DeliveryScheduleDto MapToDto(DeliverySchedule deliverySchedule)
        {
            return new DeliveryScheduleDto
            {
                Id = deliverySchedule.Id,
                OrderId = deliverySchedule.OrderId,
                DeliveryManId = deliverySchedule.DeliveryManId,
                DeliveryManName = deliverySchedule.DeliveryMan?.FullName ?? string.Empty,
                DeliveryTime = deliverySchedule.DeliveryTime,
                Address = deliverySchedule.Address,
                CustomerPhone = deliverySchedule.CustomerPhone,
                DriverContact = deliverySchedule.DriverContact
            };
        }

        private static string ResolveAccountDisplayName(Account? account)
        {
            if (account == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(account.FullName))
            {
                return account.FullName;
            }

            if (!string.IsNullOrWhiteSpace(account.Email))
            {
                return account.Email;
            }

            return string.Empty;
        }
    }
}