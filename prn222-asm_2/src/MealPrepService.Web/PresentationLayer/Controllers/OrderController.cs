using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.PresentationLayer.ViewModels;

namespace MealPrepService.Web.PresentationLayer.Controllers
{
    [Authorize(Roles = "Customer")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IMenuService _menuService;
        private readonly IVnpayService _vnpayService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderService orderService,
            IMenuService menuService,
            IVnpayService vnpayService,
            ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _menuService = menuService;
            _vnpayService = vnpayService;
            _logger = logger;
        }

        // GET: Order/Index - List customer orders
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var orderDtos = await _orderService.GetByAccountIdAsync(accountId);
                
                var viewModels = orderDtos
                    .OrderByDescending(o => o.OrderDate)
                    .Select(MapToListViewModel)
                    .ToList();
                
                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving orders for account {AccountId}", GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while loading your orders.";
                return View(new List<OrderListViewModel>());
            }
        }

        // GET: Order/Details/{id} - View order details
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var orderDto = await _orderService.GetByIdAsync(id);
                
                if (orderDto == null)
                {
                    return NotFound("Order not found.");
                }

                // Check if user owns this order
                var accountId = GetCurrentAccountId();
                if (orderDto.AccountId != accountId)
                {
                    return Forbid("You don't have permission to view this order.");
                }

                var viewModel = MapToDetailsViewModel(orderDto);
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving order {OrderId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the order details.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Order/Create - Show create order form (from today's menu)
        [HttpGet]
        public async Task<IActionResult> Create(DateTime? menuDate = null)
        {
            try
            {
                var selectedDate = menuDate ?? DateTime.Today;
                var menuDto = await _menuService.GetByDateAsync(selectedDate);
                
                if (menuDto == null || !menuDto.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = $"No active menu is available for {selectedDate:dddd, MMMM dd, yyyy}.";
                    return RedirectToAction("Today", "PublicMenu");
                }

                var viewModel = new CreateOrderViewModel
                {
                    MenuDate = selectedDate,
                    DeliveryAddress = string.Empty,
                    PreferredDeliveryTime = DateTime.Now.AddDays(1).Date.AddHours(18), // Default to 6 PM tomorrow
                    AvailableMeals = menuDto.MenuMeals
                        .Where(m => !m.IsSoldOut)
                        .Select(MapToPublicMenuMealViewModel)
                        .ToList(),
                    OrderItems = menuDto.MenuMeals
                        .Where(m => !m.IsSoldOut)
                        .Select(m => new OrderItemViewModel
                        {
                            MenuMealId = m.Id,
                            RecipeName = m.RecipeName,
                            UnitPrice = m.Price,
                            Quantity = 0,
                            AvailableQuantity = m.AvailableQuantity,
                            Recipe = MapRecipeToDetailsViewModel(m.Recipe)
                        })
                        .ToList()
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading create order form for date {MenuDate}", menuDate);
                TempData["ErrorMessage"] = "An error occurred while loading the order form.";
                return RedirectToAction("Today", "PublicMenu");
            }
        }

        // POST: Order/Create - Create order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateOrderViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload available meals for the form
                await ReloadAvailableMeals(model);
                return View(model);
            }

            try
            {
                var accountId = GetCurrentAccountId();
                
                // Filter only selected items
                var selectedItems = model.OrderItems
                    .Where(item => item.Quantity > 0)
                    .Select(item => new OrderItemDto
                    {
                        MenuMealId = item.MenuMealId,
                        Quantity = item.Quantity
                    })
                    .ToList();

                if (!selectedItems.Any())
                {
                    ModelState.AddModelError(string.Empty, "Please select at least one item to order.");
                    await ReloadAvailableMeals(model);
                    return View(model);
                }

                var orderDto = await _orderService.CreateOrderAsync(accountId, selectedItems);
                
                _logger.LogInformation("Order {OrderId} created successfully for account {AccountId}", 
                    orderDto.Id, accountId);
                
                // Store delivery information in TempData for payment page
                TempData["DeliveryAddress"] = model.DeliveryAddress;
                TempData["DeliveryNotes"] = model.DeliveryNotes;
                TempData["PreferredDeliveryTime"] = model.PreferredDeliveryTime?.ToString("yyyy-MM-dd HH:mm");
                
                return RedirectToAction(nameof(Payment), new { id = orderDto.Id });
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await ReloadAvailableMeals(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating order for account {AccountId}", GetCurrentAccountId());
                ModelState.AddModelError(string.Empty, "An error occurred while creating the order. Please try again.");
                await ReloadAvailableMeals(model);
                return View(model);
            }
        }

        // GET: Order/Payment/{id} - Show payment form
        [HttpGet]
        public async Task<IActionResult> Payment(Guid id)
        {
            try
            {
                var orderDto = await _orderService.GetByIdAsync(id);
                
                if (orderDto == null)
                {
                    return NotFound("Order not found.");
                }

                // Check if user owns this order
                var accountId = GetCurrentAccountId();
                if (orderDto.AccountId != accountId)
                {
                    return Forbid("You don't have permission to access this order.");
                }

                // Check if order can be paid
                if (!orderDto.Status.Equals("pending", StringComparison.OrdinalIgnoreCase) && 
                    !orderDto.Status.Equals("payment_failed", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "This order cannot be paid at this time.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var viewModel = new PaymentViewModel
                {
                    OrderId = id,
                    OrderTotal = orderDto.TotalAmount,
                    PaymentMethod = "Credit Card",
                    OrderDetails = orderDto.OrderDetails.Select(MapToOrderDetailDisplayViewModel).ToList(),
                    DeliveryAddress = TempData["DeliveryAddress"]?.ToString() ?? "Not specified",
                    PreferredDeliveryTime = TempData["PreferredDeliveryTime"]?.ToString() is string timeStr && DateTime.TryParse(timeStr, out var parsedTime)
                        ? parsedTime
                        : null
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading payment form for order {OrderId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the payment form.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Order/Payment - Process payment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment(PaymentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload order details for the form
                var orderDto = await _orderService.GetByIdAsync(model.OrderId);
                if (orderDto != null)
                {
                    model.OrderDetails = orderDto.OrderDetails.Select(MapToOrderDetailDisplayViewModel).ToList();
                }
                return View(model);
            }

            try
            {
                if (model.PaymentMethod == "VNPAY")
                {
                    // For VNPAY, redirect to payment gateway
                    var order = await _orderService.ProcessPaymentAsync(model.OrderId, model.PaymentMethod);
                    var paymentUrl = await _vnpayService.CreatePaymentUrlAsync(
                        model.OrderId, 
                        model.OrderTotal, 
                        $"Payment for Order {model.OrderId}");
                    
                    _logger.LogInformation("Redirecting to VNPAY for order {OrderId}", model.OrderId);
                    return Redirect(paymentUrl.PaymentUrl);
                }
                else if (model.PaymentMethod == "COD")
                {
                    // For COD, process immediately and show confirmation
                    var order = await _orderService.ProcessPaymentAsync(model.OrderId, model.PaymentMethod);
                    
                    _logger.LogInformation("COD order {OrderId} processed successfully", model.OrderId);
                    return RedirectToAction(nameof(Confirmation), new { id = order.Id });
                }
                else
                {
                    ModelState.AddModelError("", "Invalid payment method selected.");
                    
                    // Reload order details for the form
                    var orderDto = await _orderService.GetByIdAsync(model.OrderId);
                    if (orderDto != null)
                    {
                        model.OrderDetails = orderDto.OrderDetails.Select(MapToOrderDetailDisplayViewModel).ToList();
                    }
                    
                    return View(model);
                }
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                
                // Reload order details for the form
                var orderDto = await _orderService.GetByIdAsync(model.OrderId);
                if (orderDto != null)
                {
                    model.OrderDetails = orderDto.OrderDetails.Select(MapToOrderDetailDisplayViewModel).ToList();
                }
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing payment for order {OrderId}", model.OrderId);
                ModelState.AddModelError(string.Empty, "An error occurred while processing the payment. Please try again.");
                
                // Reload order details for the form
                var orderDto = await _orderService.GetByIdAsync(model.OrderId);
                if (orderDto != null)
                {
                    model.OrderDetails = orderDto.OrderDetails.Select(MapToOrderDetailDisplayViewModel).ToList();
                }
                
                return View(model);
            }
        }

        // GET: Order/Confirmation/{id} - Show order confirmation
        [HttpGet]
        public async Task<IActionResult> Confirmation(Guid id)
        {
            try
            {
                var orderDto = await _orderService.GetByIdAsync(id);
                
                if (orderDto == null)
                {
                    return NotFound("Order not found.");
                }

                // Check if user owns this order
                var accountId = GetCurrentAccountId();
                if (orderDto.AccountId != accountId)
                {
                    return Forbid("You don't have permission to view this order.");
                }

                var viewModel = MapToConfirmationViewModel(orderDto);
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading confirmation for order {OrderId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the order confirmation.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Order/Retry/{id} - Retry payment for failed order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Retry(Guid id)
        {
            try
            {
                var orderDto = await _orderService.GetByIdAsync(id);
                
                if (orderDto == null)
                {
                    return NotFound("Order not found.");
                }

                // Check if user owns this order
                var accountId = GetCurrentAccountId();
                if (orderDto.AccountId != accountId)
                {
                    return Forbid("You don't have permission to access this order.");
                }

                // Check if order can be retried
                if (!orderDto.Status.Equals("payment_failed", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "This order cannot be retried at this time.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                return RedirectToAction(nameof(Payment), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrying payment for order {OrderId}", id);
                TempData["ErrorMessage"] = "An error occurred while retrying the payment.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        #region Private Helper Methods

        private Guid GetCurrentAccountId()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdClaim) || !Guid.TryParse(accountIdClaim, out var accountId))
            {
                throw new AuthenticationException("User account ID not found in claims.");
            }
            return accountId;
        }

        private async Task ReloadAvailableMeals(CreateOrderViewModel model)
        {
            try
            {
                var menuDto = await _menuService.GetByDateAsync(model.MenuDate);
                if (menuDto != null)
                {
                    model.AvailableMeals = menuDto.MenuMeals
                        .Where(m => !m.IsSoldOut)
                        .Select(MapToPublicMenuMealViewModel)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while reloading available meals for date {MenuDate}", model.MenuDate);
            }
        }

        private OrderListViewModel MapToListViewModel(OrderDto dto)
        {
            return new OrderListViewModel
            {
                Id = dto.Id,
                OrderDate = dto.OrderDate,
                TotalAmount = dto.TotalAmount,
                PaymentMethod = dto.PaymentMethod ?? "Not specified",
                Status = dto.Status,
                ItemsCount = dto.OrderDetails.Sum(d => d.Quantity),
                DeliveryTime = dto.DeliverySchedule?.DeliveryTime,
                DeliveryAddress = dto.DeliverySchedule?.Address ?? "Not specified"
            };
        }

        private OrderDetailsViewModel MapToDetailsViewModel(OrderDto dto)
        {
            return new OrderDetailsViewModel
            {
                Id = dto.Id,
                OrderDate = dto.OrderDate,
                TotalAmount = dto.TotalAmount,
                PaymentMethod = dto.PaymentMethod ?? "Not specified",
                Status = dto.Status,
                OrderDetails = dto.OrderDetails.Select(MapToOrderDetailDisplayViewModel).ToList(),
                DeliveryInfo = dto.DeliverySchedule != null ? MapToDeliveryInfoViewModel(dto.DeliverySchedule) : null
            };
        }

        private OrderConfirmationViewModel MapToConfirmationViewModel(OrderDto dto)
        {
            return new OrderConfirmationViewModel
            {
                OrderId = dto.Id,
                OrderDate = dto.OrderDate,
                TotalAmount = dto.TotalAmount,
                PaymentMethod = dto.PaymentMethod ?? "Not specified",
                Status = dto.Status,
                OrderDetails = dto.OrderDetails.Select(MapToOrderDetailDisplayViewModel).ToList(),
                DeliveryInfo = dto.DeliverySchedule != null ? MapToDeliveryInfoViewModel(dto.DeliverySchedule) : null
            };
        }

        private OrderDetailDisplayViewModel MapToOrderDetailDisplayViewModel(OrderDetailDto dto)
        {
            return new OrderDetailDisplayViewModel
            {
                Id = dto.Id,
                RecipeName = dto.MenuMeal?.RecipeName ?? "Unknown Recipe",
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                Recipe = dto.MenuMeal?.Recipe != null ? MapRecipeToDetailsViewModel(dto.MenuMeal.Recipe) : new RecipeDetailsViewModel()
            };
        }

        private DeliveryInfoViewModel MapToDeliveryInfoViewModel(DeliveryScheduleDto dto)
        {
            return new DeliveryInfoViewModel
            {
                DeliveryTime = dto.DeliveryTime,
                Address = dto.Address,
                DriverContact = dto.DriverContact
            };
        }

        private PublicMenuMealViewModel MapToPublicMenuMealViewModel(MenuMealDto dto)
        {
            return new PublicMenuMealViewModel
            {
                Id = dto.Id,
                RecipeId = dto.RecipeId,
                RecipeName = dto.RecipeName,
                Price = dto.Price,
                AvailableQuantity = dto.AvailableQuantity,
                IsSoldOut = dto.IsSoldOut,
                Recipe = MapRecipeToDetailsViewModel(dto.Recipe)
            };
        }

        private RecipeDetailsViewModel MapRecipeToDetailsViewModel(RecipeDto dto)
        {
            return new RecipeDetailsViewModel
            {
                Id = dto.Id,
                RecipeName = dto.RecipeName,
                Instructions = dto.Instructions,
                TotalCalories = dto.TotalCalories,
                ProteinG = dto.ProteinG,
                FatG = dto.FatG,
                CarbsG = dto.CarbsG
            };
        }

        // GET: Order/VnpayCallback - Handle VNPAY payment callback
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VnpayCallback([FromQuery] VnpayCallbackDto callbackDto)
        {
            try
            {
                var order = await _orderService.ProcessVnpayCallbackAsync(callbackDto);
                
                if (order.Status == "confirmed")
                {
                    _logger.LogInformation("VNPAY payment successful for order {OrderId}", order.Id);
                    return RedirectToAction(nameof(Confirmation), new { id = order.Id });
                }
                else
                {
                    _logger.LogWarning("VNPAY payment failed for order {OrderId}", order.Id);
                    return RedirectToAction(nameof(PaymentFailed), new { id = order.Id });
                }
            }
            catch (BusinessException ex)
            {
                _logger.LogError(ex, "Business error processing VNPAY callback");
                return RedirectToAction(nameof(PaymentError), new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing VNPAY callback");
                return RedirectToAction(nameof(PaymentError), new { message = "An unexpected error occurred while processing your payment." });
            }
        }

        // GET: Order/PaymentFailed/{id} - Show payment failed page
        [HttpGet]
        public async Task<IActionResult> PaymentFailed(Guid id)
        {
            try
            {
                var orderDto = await _orderService.GetByIdAsync(id);
                
                if (orderDto == null)
                {
                    return NotFound("Order not found.");
                }

                // Check if user owns this order
                var accountId = GetCurrentAccountId();
                if (orderDto.AccountId != accountId)
                {
                    return Forbid("You don't have permission to view this order.");
                }

                var viewModel = MapToConfirmationViewModel(orderDto);
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading payment failed page for order {OrderId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the payment information.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Order/PaymentError - Show payment error page
        [HttpGet]
        public IActionResult PaymentError(string message = "")
        {
            ViewBag.ErrorMessage = string.IsNullOrEmpty(message) 
                ? "An error occurred while processing your payment. Please try again or contact support."
                : message;
            
            return View();
        }

        #endregion
    }
}