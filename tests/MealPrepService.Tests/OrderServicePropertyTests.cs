using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.Services;
using MealPrepService.DataAccessLayer.Data;
using MealPrepService.DataAccessLayer.Entities;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.Tests;

/// <summary>
/// Property-based tests for OrderService
/// Tests universal properties that should hold for all valid inputs
/// </summary>
public class OrderServicePropertyTests : IDisposable
{
    private MealPrepDbContext _context;
    private IUnitOfWork _unitOfWork;
    private IOrderService _orderService;
    private Mock<ILogger<OrderService>> _mockLogger;
    private Mock<IVnpayService> _mockVnpayService;
    private Mock<IDeliveryService> _mockDeliveryService;

    public OrderServicePropertyTests()
    {
        // Create a new in-memory database for each test
        var options = new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new MealPrepDbContext(options);
        _unitOfWork = new UnitOfWork(_context);
        _mockLogger = new Mock<ILogger<OrderService>>();
        _mockVnpayService = new Mock<IVnpayService>();
        _mockDeliveryService = new Mock<IDeliveryService>();
        
        // Setup default mock behavior for delivery service
        _mockDeliveryService
            .Setup(x => x.CreateDeliveryScheduleAsync(It.IsAny<Guid>(), It.IsAny<DeliveryScheduleDto>()))
            .ReturnsAsync((Guid orderId, DeliveryScheduleDto dto) => new DeliveryScheduleDto
            {
                Id = Guid.NewGuid(),
                OrderId = orderId,
                DeliveryTime = dto.DeliveryTime,
                Address = dto.Address,
                DriverContact = dto.DriverContact
            });
        
        _orderService = new OrderService(_unitOfWork, _mockVnpayService.Object, _mockDeliveryService.Object, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _unitOfWork.Dispose();
    }

    /// <summary>
    /// Property 40: Order detail creation
    /// For any order with menu meals, the system should create order details with quantity and unit_price
    /// Validates: Requirements 9.1
    /// </summary>
    [Property(MaxTest = 100)]
    public Property OrderDetailCreation()
    {
        return Prop.ForAll(
            GenerateValidOrderItems(1, 5),
            items =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = CreateInMemoryDbOptions();

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<OrderService>>();
                var mockVnpayService = new Mock<IVnpayService>();
                var mockDeliveryService = new Mock<IDeliveryService>();
                
                mockDeliveryService
                    .Setup(x => x.CreateDeliveryScheduleAsync(It.IsAny<Guid>(), It.IsAny<DeliveryScheduleDto>()))
                    .ReturnsAsync((Guid orderId, DeliveryScheduleDto dto) => new DeliveryScheduleDto
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        DeliveryTime = dto.DeliveryTime,
                        Address = dto.Address,
                        DriverContact = dto.DriverContact
                    });
                
                var orderService = new OrderService(unitOfWork, mockVnpayService.Object, mockDeliveryService.Object, mockLogger.Object);

                // Create test account
                var accountId = Guid.NewGuid();
                CreateTestAccount(context, accountId);

                // Create menu meals for the order items
                var menuMeals = CreateTestMenuMeals(context, items.Count);
                
                // Map items to menu meals
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].MenuMealId = menuMeals[i].Id;
                }

                // Act: Create order
                var order = orderService.CreateOrderAsync(accountId, items).Result;

                // Assert: Order should have correct number of details with quantity and unit price
                return order.OrderDetails.Count == items.Count
                    && order.OrderDetails.All(d => d.Quantity > 0)
                    && order.OrderDetails.All(d => d.UnitPrice > 0)
                    && order.OrderDetails.All(d => d.MenuMealId != Guid.Empty);
            });
    }

    /// <summary>
    /// Property 41: Order quantity validation
    /// For any order item, if the requested quantity exceeds available_quantity, 
    /// the order creation should be rejected
    /// Validates: Requirements 9.2
    /// </summary>
    [Property(MaxTest = 100)]
    public Property OrderQuantityValidation()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 10)),
            Arb.From(Gen.Choose(11, 20)),
            (availableQty, requestedQty) =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = CreateInMemoryDbOptions();

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<OrderService>>();
                var mockVnpayService = new Mock<IVnpayService>();
                var mockDeliveryService = new Mock<IDeliveryService>();
                
                mockDeliveryService
                    .Setup(x => x.CreateDeliveryScheduleAsync(It.IsAny<Guid>(), It.IsAny<DeliveryScheduleDto>()))
                    .ReturnsAsync((Guid orderId, DeliveryScheduleDto dto) => new DeliveryScheduleDto
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        DeliveryTime = dto.DeliveryTime,
                        Address = dto.Address,
                        DriverContact = dto.DriverContact
                    });
                
                var orderService = new OrderService(unitOfWork, mockVnpayService.Object, mockDeliveryService.Object, mockLogger.Object);

                // Create test account
                var accountId = Guid.NewGuid();
                CreateTestAccount(context, accountId);

                // Create menu meal with limited quantity
                var menuMeal = CreateTestMenuMeal(context, availableQty);

                // Create order item requesting more than available
                var items = new List<OrderItemDto>
                {
                    new OrderItemDto
                    {
                        MenuMealId = menuMeal.Id,
                        Quantity = requestedQty
                    }
                };

                // Act & Assert: Should throw BusinessException
                try
                {
                    orderService.CreateOrderAsync(accountId, items).Wait();
                    return false; // Should not reach here
                }
                catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                {
                    return ex.Message.Contains("Insufficient quantity");
                }
                catch
                {
                    return false; // Wrong exception type
                }
            });
    }

    /// <summary>
    /// Property 42: Order total calculation
    /// For any order, the total_amount should equal the sum of (quantity × unit_price) for all order details
    /// Validates: Requirements 9.3
    /// </summary>
    [Property(MaxTest = 100)]
    public Property OrderTotalCalculation()
    {
        return Prop.ForAll(
            GenerateValidOrderItems(1, 5),
            items =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = CreateInMemoryDbOptions();

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<OrderService>>();
                var mockVnpayService = new Mock<IVnpayService>();
                var mockDeliveryService = new Mock<IDeliveryService>();
                
                mockDeliveryService
                    .Setup(x => x.CreateDeliveryScheduleAsync(It.IsAny<Guid>(), It.IsAny<DeliveryScheduleDto>()))
                    .ReturnsAsync((Guid orderId, DeliveryScheduleDto dto) => new DeliveryScheduleDto
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        DeliveryTime = dto.DeliveryTime,
                        Address = dto.Address,
                        DriverContact = dto.DriverContact
                    });
                
                var orderService = new OrderService(unitOfWork, mockVnpayService.Object, mockDeliveryService.Object, mockLogger.Object);

                // Create test account
                var accountId = Guid.NewGuid();
                CreateTestAccount(context, accountId);

                // Create menu meals for the order items
                var menuMeals = CreateTestMenuMeals(context, items.Count);
                
                // Map items to menu meals
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].MenuMealId = menuMeals[i].Id;
                }

                // Act: Create order
                var order = orderService.CreateOrderAsync(accountId, items).Result;

                // Calculate expected total
                decimal expectedTotal = 0;
                foreach (var detail in order.OrderDetails)
                {
                    expectedTotal += detail.Quantity * detail.UnitPrice;
                }

                // Assert: Order total should match sum of details
                return order.TotalAmount == expectedTotal;
            });
    }

    /// <summary>
    /// Property 43: Inventory reduction on order
    /// For any order placed, each menu meal's available_quantity should be reduced by the ordered quantity
    /// Validates: Requirements 9.4
    /// </summary>
    [Property(MaxTest = 100)]
    public Property InventoryReductionOnOrder()
    {
        return Prop.ForAll(
            GenerateValidOrderItems(1, 3),
            items =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = CreateInMemoryDbOptions();

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<OrderService>>();
                var mockVnpayService = new Mock<IVnpayService>();
                var mockDeliveryService = new Mock<IDeliveryService>();
                
                mockDeliveryService
                    .Setup(x => x.CreateDeliveryScheduleAsync(It.IsAny<Guid>(), It.IsAny<DeliveryScheduleDto>()))
                    .ReturnsAsync((Guid orderId, DeliveryScheduleDto dto) => new DeliveryScheduleDto
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        DeliveryTime = dto.DeliveryTime,
                        Address = dto.Address,
                        DriverContact = dto.DriverContact
                    });
                
                var orderService = new OrderService(unitOfWork, mockVnpayService.Object, mockDeliveryService.Object, mockLogger.Object);

                // Create test account
                var accountId = Guid.NewGuid();
                CreateTestAccount(context, accountId);

                // Create menu meals for the order items
                var menuMeals = CreateTestMenuMeals(context, items.Count);
                
                // Store original quantities
                var originalQuantities = new Dictionary<Guid, int>();
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].MenuMealId = menuMeals[i].Id;
                    originalQuantities[menuMeals[i].Id] = menuMeals[i].AvailableQuantity;
                }

                // Act: Create order
                var order = orderService.CreateOrderAsync(accountId, items).Result;

                // Assert: Check that quantities were reduced correctly
                bool allQuantitiesReduced = true;
                foreach (var item in items)
                {
                    var menuMeal = unitOfWork.MenuMeals.GetByIdAsync(item.MenuMealId).Result;
                    var expectedQuantity = originalQuantities[item.MenuMealId] - item.Quantity;
                    
                    if (menuMeal == null || menuMeal.AvailableQuantity != expectedQuantity)
                    {
                        allQuantitiesReduced = false;
                        break;
                    }
                }

                return allQuantitiesReduced;
            });
    }

    /// <summary>
    /// Property 44: Payment failure rollback
    /// For any payment that fails, menu meal quantities should be restored to their original values
    /// Validates: Requirements 9.6
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PaymentFailureRollback()
    {
        return Prop.ForAll(
            GenerateValidOrderItems(1, 3),
            items =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = CreateInMemoryDbOptions();

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<OrderService>>();
                var mockVnpayService = new Mock<IVnpayService>();
                var mockDeliveryService = new Mock<IDeliveryService>();
                
                mockDeliveryService
                    .Setup(x => x.CreateDeliveryScheduleAsync(It.IsAny<Guid>(), It.IsAny<DeliveryScheduleDto>()))
                    .ReturnsAsync((Guid orderId, DeliveryScheduleDto dto) => new DeliveryScheduleDto
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        DeliveryTime = dto.DeliveryTime,
                        Address = dto.Address,
                        DriverContact = dto.DriverContact
                    });
                
                // Setup VNPAY service to return failure
                mockVnpayService
                    .Setup(x => x.ProcessCallbackAsync(It.IsAny<VnpayCallbackDto>()))
                    .ReturnsAsync((VnpayCallbackDto dto) => new VnpayCallbackResult
                    {
                        IsSuccess = true,
                        OrderId = Guid.Parse(dto.vnp_TxnRef),
                        TransactionId = dto.vnp_TransactionNo,
                        ResponseCode = "01", // Failure code
                        Message = "Payment failed"
                    });
                
                var orderService = new OrderService(unitOfWork, mockVnpayService.Object, mockDeliveryService.Object, mockLogger.Object);

                // Create test account
                var accountId = Guid.NewGuid();
                CreateTestAccount(context, accountId);

                // Create menu meals for the order items
                var menuMeals = CreateTestMenuMeals(context, items.Count);
                
                // Store original quantities
                var originalQuantities = new Dictionary<Guid, int>();
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].MenuMealId = menuMeals[i].Id;
                    originalQuantities[menuMeals[i].Id] = menuMeals[i].AvailableQuantity;
                }

                // Act: Create order
                var order = orderService.CreateOrderAsync(accountId, items).Result;
                
                // Process payment with VNPAY (which will fail)
                var processedOrder = orderService.ProcessPaymentAsync(order.Id, "VNPAY").Result;
                
                // Simulate VNPAY callback with failure
                var callbackDto = new VnpayCallbackDto
                {
                    vnp_TxnRef = order.Id.ToString(),
                    vnp_TransactionNo = "TEST123",
                    vnp_ResponseCode = "01",
                    vnp_TransactionStatus = "01"
                };
                
                var callbackResult = orderService.ProcessVnpayCallbackAsync(callbackDto).Result;

                // Assert: Check that quantities were restored
                bool allQuantitiesRestored = true;
                foreach (var item in items)
                {
                    var menuMeal = unitOfWork.MenuMeals.GetByIdAsync(item.MenuMealId).Result;
                    
                    if (menuMeal == null || menuMeal.AvailableQuantity != originalQuantities[item.MenuMealId])
                    {
                        allQuantitiesRestored = false;
                        break;
                    }
                }

                return allQuantitiesRestored && callbackResult.Status == "payment_failed";
            });
    }

    /// <summary>
    /// Property 45: Payment success workflow
    /// For any successful payment (VNPAY or COD confirmation), order status should be "confirmed" 
    /// and delivery schedule should be created
    /// Validates: Requirements 9.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PaymentSuccessWorkflow()
    {
        return Prop.ForAll(
            GenerateValidOrderItems(1, 3),
            Arb.From(Gen.Elements("COD", "VNPAY")),
            (items, paymentMethod) =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = CreateInMemoryDbOptions();

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<OrderService>>();
                var mockVnpayService = new Mock<IVnpayService>();
                var mockDeliveryService = new Mock<IDeliveryService>();
                
                bool deliveryScheduleCreated = false;
                mockDeliveryService
                    .Setup(x => x.CreateDeliveryScheduleAsync(It.IsAny<Guid>(), It.IsAny<DeliveryScheduleDto>()))
                    .Callback(() => deliveryScheduleCreated = true)
                    .ReturnsAsync((Guid orderId, DeliveryScheduleDto dto) => new DeliveryScheduleDto
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        DeliveryTime = dto.DeliveryTime,
                        Address = dto.Address,
                        DriverContact = dto.DriverContact
                    });
                
                // Setup VNPAY service to return success
                mockVnpayService
                    .Setup(x => x.ProcessCallbackAsync(It.IsAny<VnpayCallbackDto>()))
                    .ReturnsAsync((VnpayCallbackDto dto) => new VnpayCallbackResult
                    {
                        IsSuccess = true,
                        OrderId = Guid.Parse(dto.vnp_TxnRef),
                        TransactionId = dto.vnp_TransactionNo,
                        ResponseCode = "00", // Success code
                        Message = "Payment successful"
                    });
                
                var orderService = new OrderService(unitOfWork, mockVnpayService.Object, mockDeliveryService.Object, mockLogger.Object);

                // Create test account
                var accountId = Guid.NewGuid();
                CreateTestAccount(context, accountId);

                // Create menu meals for the order items
                var menuMeals = CreateTestMenuMeals(context, items.Count);
                
                // Map items to menu meals
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].MenuMealId = menuMeals[i].Id;
                }

                // Act: Create order
                var order = orderService.CreateOrderAsync(accountId, items).Result;
                
                // Process payment
                var processedOrder = orderService.ProcessPaymentAsync(order.Id, paymentMethod).Result;
                
                OrderDto finalOrder;
                if (paymentMethod == "COD")
                {
                    // For COD, confirm cash payment by delivery man
                    var deliveryManId = Guid.NewGuid();
                    finalOrder = orderService.ConfirmCashPaymentAsync(order.Id, deliveryManId).Result;
                }
                else // VNPAY
                {
                    // Simulate VNPAY callback with success
                    var callbackDto = new VnpayCallbackDto
                    {
                        vnp_TxnRef = order.Id.ToString(),
                        vnp_TransactionNo = "TEST123",
                        vnp_ResponseCode = "00",
                        vnp_TransactionStatus = "00"
                    };
                    
                    finalOrder = orderService.ProcessVnpayCallbackAsync(callbackDto).Result;
                }

                // Assert: Order should be confirmed and delivery schedule created
                return finalOrder.Status == "confirmed"
                    && deliveryScheduleCreated
                    && finalOrder.PaymentConfirmedAt.HasValue;
            });
    }

    /// <summary>
    /// Property 69: COD order status workflow
    /// For any COD order, the status workflow should be: pending → pending_payment → confirmed
    /// Validates: Requirements 9.5, 9.10, 10.6
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CodOrderStatusWorkflow()
    {
        return Prop.ForAll(
            GenerateValidOrderItems(1, 3),
            items =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = CreateInMemoryDbOptions();

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<OrderService>>();
                var mockVnpayService = new Mock<IVnpayService>();
                var mockDeliveryService = new Mock<IDeliveryService>();
                
                mockDeliveryService
                    .Setup(x => x.CreateDeliveryScheduleAsync(It.IsAny<Guid>(), It.IsAny<DeliveryScheduleDto>()))
                    .ReturnsAsync((Guid orderId, DeliveryScheduleDto dto) => new DeliveryScheduleDto
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        DeliveryTime = dto.DeliveryTime,
                        Address = dto.Address,
                        DriverContact = dto.DriverContact
                    });
                
                var orderService = new OrderService(unitOfWork, mockVnpayService.Object, mockDeliveryService.Object, mockLogger.Object);

                // Create test account
                var accountId = Guid.NewGuid();
                CreateTestAccount(context, accountId);

                // Create menu meals for the order items
                var menuMeals = CreateTestMenuMeals(context, items.Count);
                
                // Map items to menu meals
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].MenuMealId = menuMeals[i].Id;
                }

                // Act: Create order (status should be "pending")
                var order = orderService.CreateOrderAsync(accountId, items).Result;
                var statusAfterCreate = order.Status;
                
                // Process payment with COD (status should be "pending_payment")
                var processedOrder = orderService.ProcessPaymentAsync(order.Id, "COD").Result;
                var statusAfterPayment = processedOrder.Status;
                
                // Confirm cash payment by delivery man (status should be "confirmed")
                var deliveryManId = Guid.NewGuid();
                var confirmedOrder = orderService.ConfirmCashPaymentAsync(order.Id, deliveryManId).Result;
                var statusAfterConfirmation = confirmedOrder.Status;

                // Assert: Verify status workflow
                return statusAfterCreate == "pending"
                    && statusAfterPayment == "pending_payment"
                    && statusAfterConfirmation == "confirmed"
                    && confirmedOrder.PaymentMethod == "COD"
                    && confirmedOrder.PaymentConfirmedAt.HasValue
                    && confirmedOrder.PaymentConfirmedBy == deliveryManId;
            });
    }

    /// <summary>
    /// Property 70: VNPAY payment callback processing
    /// For any VNPAY callback with success code "00", order status should be "confirmed" 
    /// and delivery schedule should be created
    /// Validates: Requirements 9.6, 9.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property VnpayPaymentCallbackProcessing()
    {
        return Prop.ForAll(
            GenerateValidOrderItems(1, 3),
            items =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = CreateInMemoryDbOptions();

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<OrderService>>();
                var mockVnpayService = new Mock<IVnpayService>();
                var mockDeliveryService = new Mock<IDeliveryService>();
                
                bool deliveryScheduleCreated = false;
                mockDeliveryService
                    .Setup(x => x.CreateDeliveryScheduleAsync(It.IsAny<Guid>(), It.IsAny<DeliveryScheduleDto>()))
                    .Callback(() => deliveryScheduleCreated = true)
                    .ReturnsAsync((Guid orderId, DeliveryScheduleDto dto) => new DeliveryScheduleDto
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        DeliveryTime = dto.DeliveryTime,
                        Address = dto.Address,
                        DriverContact = dto.DriverContact
                    });
                
                // Setup VNPAY service to return success
                mockVnpayService
                    .Setup(x => x.ProcessCallbackAsync(It.IsAny<VnpayCallbackDto>()))
                    .ReturnsAsync((VnpayCallbackDto dto) => new VnpayCallbackResult
                    {
                        IsSuccess = true,
                        OrderId = Guid.Parse(dto.vnp_TxnRef),
                        TransactionId = dto.vnp_TransactionNo,
                        ResponseCode = "00", // Success code
                        Message = "Payment successful"
                    });
                
                var orderService = new OrderService(unitOfWork, mockVnpayService.Object, mockDeliveryService.Object, mockLogger.Object);

                // Create test account
                var accountId = Guid.NewGuid();
                CreateTestAccount(context, accountId);

                // Create menu meals for the order items
                var menuMeals = CreateTestMenuMeals(context, items.Count);
                
                // Map items to menu meals
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].MenuMealId = menuMeals[i].Id;
                }

                // Act: Create order
                var order = orderService.CreateOrderAsync(accountId, items).Result;
                
                // Process payment with VNPAY
                var processedOrder = orderService.ProcessPaymentAsync(order.Id, "VNPAY").Result;
                
                // Simulate VNPAY callback with success
                var callbackDto = new VnpayCallbackDto
                {
                    vnp_TxnRef = order.Id.ToString(),
                    vnp_TransactionNo = "TEST123456",
                    vnp_ResponseCode = "00",
                    vnp_TransactionStatus = "00"
                };
                
                var finalOrder = orderService.ProcessVnpayCallbackAsync(callbackDto).Result;

                // Assert: Order should be confirmed, delivery schedule created, and transaction ID stored
                return finalOrder.Status == "confirmed"
                    && deliveryScheduleCreated
                    && finalOrder.VnpayTransactionId == "TEST123456"
                    && finalOrder.PaymentConfirmedAt.HasValue
                    && finalOrder.PaymentMethod == "VNPAY";
            });
    }

    /// <summary>
    /// Property 71: Cash payment confirmation authorization
    /// For any COD order, only orders with status "pending_payment" and payment method "COD" 
    /// should allow cash payment confirmation
    /// Validates: Requirements 9.10, 10.6, 10.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property CashPaymentConfirmationAuthorization()
    {
        return Prop.ForAll(
            GenerateValidOrderItems(1, 3),
            Arb.From(Gen.Elements("pending", "confirmed", "delivered", "payment_failed")),
            (items, invalidStatus) =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = CreateInMemoryDbOptions();

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<OrderService>>();
                var mockVnpayService = new Mock<IVnpayService>();
                var mockDeliveryService = new Mock<IDeliveryService>();
                
                mockDeliveryService
                    .Setup(x => x.CreateDeliveryScheduleAsync(It.IsAny<Guid>(), It.IsAny<DeliveryScheduleDto>()))
                    .ReturnsAsync((Guid orderId, DeliveryScheduleDto dto) => new DeliveryScheduleDto
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        DeliveryTime = dto.DeliveryTime,
                        Address = dto.Address,
                        DriverContact = dto.DriverContact
                    });
                
                var orderService = new OrderService(unitOfWork, mockVnpayService.Object, mockDeliveryService.Object, mockLogger.Object);

                // Create test account
                var accountId = Guid.NewGuid();
                CreateTestAccount(context, accountId);

                // Create menu meals for the order items
                var menuMeals = CreateTestMenuMeals(context, items.Count);
                
                // Map items to menu meals
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].MenuMealId = menuMeals[i].Id;
                }

                // Act: Create order and set it to an invalid status
                var order = orderService.CreateOrderAsync(accountId, items).Result;
                
                // Manually set order to invalid status for testing
                var orderEntity = unitOfWork.Orders.GetByIdAsync(order.Id).Result;
                if (orderEntity != null)
                {
                    orderEntity.Status = invalidStatus;
                    orderEntity.PaymentMethod = "COD";
                    unitOfWork.Orders.UpdateAsync(orderEntity).Wait();
                    unitOfWork.SaveChangesAsync().Wait();
                }
                
                // Try to confirm cash payment with invalid status
                var deliveryManId = Guid.NewGuid();
                
                try
                {
                    orderService.ConfirmCashPaymentAsync(order.Id, deliveryManId).Wait();
                    return false; // Should not reach here
                }
                catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                {
                    return ex.Message.Contains("Cannot confirm payment");
                }
                catch
                {
                    return false; // Wrong exception type
                }
            });
    }

    /// <summary>
    /// Property 72: Payment method validation
    /// For any order payment processing, only valid payment methods ("COD" or "VNPAY") should be accepted
    /// Validates: Requirements 9.5
    /// </summary>
    [Property(MaxTest = 100)]
    public Property PaymentMethodValidation()
    {
        return Prop.ForAll(
            GenerateValidOrderItems(1, 3),
            Arb.From(Gen.Elements("CREDIT_CARD", "PAYPAL", "BANK_TRANSFER", "INVALID", "")),
            (items, invalidPaymentMethod) =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = CreateInMemoryDbOptions();

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<OrderService>>();
                var mockVnpayService = new Mock<IVnpayService>();
                var mockDeliveryService = new Mock<IDeliveryService>();
                
                mockDeliveryService
                    .Setup(x => x.CreateDeliveryScheduleAsync(It.IsAny<Guid>(), It.IsAny<DeliveryScheduleDto>()))
                    .ReturnsAsync((Guid orderId, DeliveryScheduleDto dto) => new DeliveryScheduleDto
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        DeliveryTime = dto.DeliveryTime,
                        Address = dto.Address,
                        DriverContact = dto.DriverContact
                    });
                
                var orderService = new OrderService(unitOfWork, mockVnpayService.Object, mockDeliveryService.Object, mockLogger.Object);

                // Create test account
                var accountId = Guid.NewGuid();
                CreateTestAccount(context, accountId);

                // Create menu meals for the order items
                var menuMeals = CreateTestMenuMeals(context, items.Count);
                
                // Map items to menu meals
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].MenuMealId = menuMeals[i].Id;
                }

                // Act: Create order
                var order = orderService.CreateOrderAsync(accountId, items).Result;
                
                // Try to process payment with invalid payment method
                try
                {
                    orderService.ProcessPaymentAsync(order.Id, invalidPaymentMethod).Wait();
                    return false; // Should not reach here
                }
                catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                {
                    return ex.Message.Contains("Invalid payment method") || ex.Message.Contains("Payment method is required");
                }
                catch
                {
                    return false; // Wrong exception type
                }
            });
    }

    /// <summary>
    /// Property 73: VNPAY callback validation
    /// For any VNPAY callback, the secure hash must be validated before processing
    /// Invalid callbacks should be rejected
    /// Validates: Requirements 9.6, 9.7
    /// </summary>
    [Property(MaxTest = 100)]
    public Property VnpayCallbackValidation()
    {
        return Prop.ForAll(
            GenerateValidOrderItems(1, 3),
            items =>
            {
                // Arrange: Create a fresh context for this test iteration
                var options = CreateInMemoryDbOptions();

                using var context = new MealPrepDbContext(options);
                using var unitOfWork = new UnitOfWork(context);
                var mockLogger = new Mock<ILogger<OrderService>>();
                var mockVnpayService = new Mock<IVnpayService>();
                var mockDeliveryService = new Mock<IDeliveryService>();
                
                mockDeliveryService
                    .Setup(x => x.CreateDeliveryScheduleAsync(It.IsAny<Guid>(), It.IsAny<DeliveryScheduleDto>()))
                    .ReturnsAsync((Guid orderId, DeliveryScheduleDto dto) => new DeliveryScheduleDto
                    {
                        Id = Guid.NewGuid(),
                        OrderId = orderId,
                        DeliveryTime = dto.DeliveryTime,
                        Address = dto.Address,
                        DriverContact = dto.DriverContact
                    });
                
                // Setup VNPAY service to return validation failure
                mockVnpayService
                    .Setup(x => x.ProcessCallbackAsync(It.IsAny<VnpayCallbackDto>()))
                    .ReturnsAsync((VnpayCallbackDto dto) => new VnpayCallbackResult
                    {
                        IsSuccess = false,
                        Message = "Invalid callback signature"
                    });
                
                var orderService = new OrderService(unitOfWork, mockVnpayService.Object, mockDeliveryService.Object, mockLogger.Object);

                // Create test account
                var accountId = Guid.NewGuid();
                CreateTestAccount(context, accountId);

                // Create menu meals for the order items
                var menuMeals = CreateTestMenuMeals(context, items.Count);
                
                // Map items to menu meals
                for (int i = 0; i < items.Count; i++)
                {
                    items[i].MenuMealId = menuMeals[i].Id;
                }

                // Act: Create order
                var order = orderService.CreateOrderAsync(accountId, items).Result;
                
                // Process payment with VNPAY
                var processedOrder = orderService.ProcessPaymentAsync(order.Id, "VNPAY").Result;
                
                // Simulate VNPAY callback with invalid signature
                var callbackDto = new VnpayCallbackDto
                {
                    vnp_TxnRef = order.Id.ToString(),
                    vnp_TransactionNo = "TEST123456",
                    vnp_ResponseCode = "00",
                    vnp_TransactionStatus = "00",
                    vnp_SecureHash = "INVALID_HASH"
                };
                
                // Try to process invalid callback
                try
                {
                    orderService.ProcessVnpayCallbackAsync(callbackDto).Wait();
                    return false; // Should not reach here
                }
                catch (AggregateException ae) when (ae.InnerException is BusinessException ex)
                {
                    return ex.Message.Contains("Invalid VNPAY callback");
                }
                catch
                {
                    return false; // Wrong exception type
                }
            });
    }

    #region Helper Methods

    private static DbContextOptions<MealPrepDbContext> CreateInMemoryDbOptions()
    {
        return new DbContextOptionsBuilder<MealPrepDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
    }

    private Account CreateTestAccount(MealPrepDbContext context, Guid accountId)
    {
        var account = new Account
        {
            Id = accountId,
            Email = $"test{accountId}@example.com",
            PasswordHash = "hashedpassword",
            FullName = "Test User",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };
        context.Accounts.Add(account);
        context.SaveChanges();
        return account;
    }

    private MenuMeal CreateTestMenuMeal(MealPrepDbContext context, int availableQuantity)
    {
        // Create recipe
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            RecipeName = $"Recipe{Guid.NewGuid()}",
            Instructions = "Test instructions",
            TotalCalories = 500,
            ProteinG = 20,
            FatG = 15,
            CarbsG = 60,
            CreatedAt = DateTime.UtcNow
        };
        context.Recipes.Add(recipe);

        // Create daily menu
        var menu = new DailyMenu
        {
            Id = Guid.NewGuid(),
            MenuDate = DateTime.Today,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };
        context.DailyMenus.Add(menu);

        // Create menu meal
        var menuMeal = new MenuMeal
        {
            Id = Guid.NewGuid(),
            MenuId = menu.Id,
            RecipeId = recipe.Id,
            Price = 10.00m + (decimal)(new System.Random().NextDouble() * 20),
            AvailableQuantity = availableQuantity,
            CreatedAt = DateTime.UtcNow,
            Recipe = recipe,
            Menu = menu
        };
        context.MenuMeals.Add(menuMeal);
        context.SaveChanges();
        
        return menuMeal;
    }

    private List<MenuMeal> CreateTestMenuMeals(MealPrepDbContext context, int count)
    {
        var menuMeals = new List<MenuMeal>();
        
        // Create daily menu
        var menu = new DailyMenu
        {
            Id = Guid.NewGuid(),
            MenuDate = DateTime.Today,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };
        context.DailyMenus.Add(menu);
        
        for (int i = 0; i < count; i++)
        {
            // Create recipe
            var recipe = new Recipe
            {
                Id = Guid.NewGuid(),
                RecipeName = $"Recipe{i}",
                Instructions = $"Instructions for recipe {i}",
                TotalCalories = 300 + (i * 50),
                ProteinG = 20 + (i * 2),
                FatG = 10 + i,
                CarbsG = 30 + (i * 3),
                CreatedAt = DateTime.UtcNow
            };
            context.Recipes.Add(recipe);

            // Create menu meal with sufficient quantity
            var menuMeal = new MenuMeal
            {
                Id = Guid.NewGuid(),
                MenuId = menu.Id,
                RecipeId = recipe.Id,
                Price = 10.00m + (i * 2),
                AvailableQuantity = 50, // Sufficient quantity for testing
                CreatedAt = DateTime.UtcNow,
                Recipe = recipe,
                Menu = menu
            };
            context.MenuMeals.Add(menuMeal);
            menuMeals.Add(menuMeal);
        }
        
        context.SaveChanges();
        return menuMeals;
    }

    #endregion

    #region Generators

    /// <summary>
    /// Generator for valid order items
    /// </summary>
    private static Arbitrary<List<OrderItemDto>> GenerateValidOrderItems(int minCount, int maxCount)
    {
        var gen = from count in Gen.Choose(minCount, maxCount)
                  from items in Gen.ListOf(count, GenerateValidOrderItem().Generator)
                  select items.ToList();

        return Arb.From(gen);
    }

    /// <summary>
    /// Generator for a single valid order item
    /// </summary>
    private static Arbitrary<OrderItemDto> GenerateValidOrderItem()
    {
        var gen = from quantity in Gen.Choose(1, 10)
                  select new OrderItemDto
                  {
                      MenuMealId = Guid.NewGuid(), // Will be replaced with actual menu meal ID
                      Quantity = quantity
                  };

        return Arb.From(gen);
    }

    #endregion
}
