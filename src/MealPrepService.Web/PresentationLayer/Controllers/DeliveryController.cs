using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.PresentationLayer.ViewModels;

namespace MealPrepService.Web.PresentationLayer.Controllers
{
    [Authorize(Roles = "Customer,DeliveryMan")]
    public class DeliveryController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly IDeliveryService _deliveryService;
        private readonly ILogger<DeliveryController> _logger;

        public DeliveryController(
            IOrderService orderService,
            IDeliveryService deliveryService,
            ILogger<DeliveryController> logger)
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _deliveryService = deliveryService ?? throw new ArgumentNullException(nameof(deliveryService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: Delivery/MyDeliveries - Show customer's delivery schedules
        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyDeliveries()
        {
            try
            {
                var customerId = GetCurrentAccountId();
                var deliveries = await _deliveryService.GetByAccountIdAsync(customerId);
                
                var upcomingDeliveries = deliveries
                    .Where(d => d.Order?.Status != "delivered")
                    .Select(MapToDeliveryScheduleViewModel)
                    .OrderBy(d => d.DeliveryTime)
                    .ToList();
                
                var completedDeliveries = deliveries
                    .Where(d => d.Order?.Status == "delivered")
                    .Select(MapToDeliveryScheduleViewModel)
                    .OrderByDescending(d => d.DeliveryTime)
                    .ToList();
                
                var viewModel = new MyDeliveriesViewModel
                {
                    UpcomingDeliveries = upcomingDeliveries,
                    CompletedDeliveries = completedDeliveries
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving deliveries for customer {CustomerId}", GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while loading your deliveries.";
                return View(new MyDeliveriesViewModel());
            }
        }

        // GET: Delivery/AssignedDeliveries - Show delivery man's assigned orders
        [HttpGet]
        [Authorize(Roles = "DeliveryMan")]
        public async Task<IActionResult> AssignedDeliveries()
        {
            try
            {
                var deliveryManId = GetCurrentAccountId();
                var deliveries = await _deliveryService.GetByDeliveryManAsync(deliveryManId);
                
                var deliveryViewModels = deliveries
                    .Select(MapToDeliveryScheduleViewModel)
                    .OrderBy(d => d.DeliveryTime)
                    .ToList();
                
                var viewModel = new AssignedDeliveriesViewModel
                {
                    Deliveries = deliveryViewModels
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving assigned deliveries for delivery man {DeliveryManId}", GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while loading your assigned deliveries.";
                return View(new AssignedDeliveriesViewModel());
            }
        }

        // POST: Delivery/ConfirmCashPayment - Confirm cash payment for COD orders
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "DeliveryMan")]
        public async Task<IActionResult> ConfirmCashPayment(Guid orderId)
        {
            try
            {
                var deliveryManId = GetCurrentAccountId();
                var order = await _orderService.ConfirmCashPaymentAsync(orderId, deliveryManId);
                
                TempData["SuccessMessage"] = "Cash payment confirmed successfully.";
                _logger.LogInformation("Cash payment confirmed for order {OrderId} by delivery man {DeliveryManId}", 
                    orderId, deliveryManId);
                
                return RedirectToAction(nameof(AssignedDeliveries));
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                _logger.LogWarning(ex, "Business error confirming cash payment for order {OrderId}", orderId);
                return RedirectToAction(nameof(AssignedDeliveries));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while confirming the cash payment. Please try again.";
                _logger.LogError(ex, "Unexpected error confirming cash payment for order {OrderId}", orderId);
                return RedirectToAction(nameof(AssignedDeliveries));
            }
        }

        // POST: Delivery/CompleteDelivery - Complete delivery
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "DeliveryMan")]
        public async Task<IActionResult> CompleteDelivery(Guid deliveryId)
        {
            try
            {
                await _deliveryService.CompleteDeliveryAsync(deliveryId);
                
                TempData["SuccessMessage"] = "Delivery completed successfully.";
                _logger.LogInformation("Delivery {DeliveryId} completed by delivery man {DeliveryManId}", 
                    deliveryId, GetCurrentAccountId());
                
                return RedirectToAction(nameof(AssignedDeliveries));
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                _logger.LogWarning(ex, "Business error completing delivery {DeliveryId}", deliveryId);
                return RedirectToAction(nameof(AssignedDeliveries));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while completing the delivery. Please try again.";
                _logger.LogError(ex, "Unexpected error completing delivery {DeliveryId}", deliveryId);
                return RedirectToAction(nameof(AssignedDeliveries));
            }
        }

        // GET: Delivery/UpdateTime/{deliveryId} - Show form to update delivery time
        [HttpGet]
        [Authorize(Roles = "DeliveryMan")]
        public async Task<IActionResult> UpdateTime(Guid deliveryId)
        {
            try
            {
                var deliveryManId = GetCurrentAccountId();
                var deliveries = await _deliveryService.GetByDeliveryManAsync(deliveryManId);
                var delivery = deliveries.FirstOrDefault(d => d.Id == deliveryId);
                
                if (delivery == null)
                {
                    TempData["ErrorMessage"] = "Delivery not found or you don't have permission to update it.";
                    return RedirectToAction(nameof(AssignedDeliveries));
                }
                
                var viewModel = new UpdateDeliveryTimeViewModel
                {
                    DeliveryId = delivery.Id,
                    OrderId = delivery.OrderId,
                    CurrentDeliveryTime = delivery.DeliveryTime,
                    NewDeliveryTime = delivery.DeliveryTime.AddHours(1), // Default to 1 hour later
                    Address = delivery.Address
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading update delivery time form for delivery {DeliveryId}", deliveryId);
                TempData["ErrorMessage"] = "An error occurred while loading the update form.";
                return RedirectToAction(nameof(AssignedDeliveries));
            }
        }

        // POST: Delivery/UpdateTime - Update delivery time
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "DeliveryMan")]
        public async Task<IActionResult> UpdateTime(UpdateDeliveryTimeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            
            try
            {
                await _deliveryService.UpdateDeliveryTimeAsync(model.DeliveryId, model.NewDeliveryTime);
                
                TempData["SuccessMessage"] = "Delivery time updated successfully.";
                _logger.LogInformation("Delivery time updated for delivery {DeliveryId} to {NewTime} by delivery man {DeliveryManId}", 
                    model.DeliveryId, model.NewDeliveryTime, GetCurrentAccountId());
                
                return RedirectToAction(nameof(AssignedDeliveries));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError("", ex.Message);
                _logger.LogWarning(ex, "Business error updating delivery time for delivery {DeliveryId}", model.DeliveryId);
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating the delivery time. Please try again.");
                _logger.LogError(ex, "Unexpected error updating delivery time for delivery {DeliveryId}", model.DeliveryId);
                return View(model);
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

        private DeliveryScheduleViewModel MapToDeliveryScheduleViewModel(DeliveryScheduleDto dto)
        {
            return new DeliveryScheduleViewModel
            {
                Id = dto.Id,
                OrderId = dto.OrderId,
                DeliveryTime = dto.DeliveryTime,
                Address = dto.Address,
                DriverContact = dto.DriverContact,
                OrderStatus = dto.Order?.Status ?? "Unknown",
                PaymentMethod = dto.Order?.PaymentMethod ?? "Unknown",
                TotalAmount = dto.Order?.TotalAmount ?? 0
            };
        }

        #endregion
    }
}