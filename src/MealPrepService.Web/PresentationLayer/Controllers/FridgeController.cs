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
    public class FridgeController : Controller
    {
        private readonly IFridgeService _fridgeService;
        private readonly IIngredientService _ingredientService;
        private readonly IMealPlanService _mealPlanService;
        private readonly ILogger<FridgeController> _logger;

        public FridgeController(
            IFridgeService fridgeService,
            IIngredientService ingredientService,
            IMealPlanService mealPlanService,
            ILogger<FridgeController> logger)
        {
            _fridgeService = fridgeService;
            _ingredientService = ingredientService;
            _mealPlanService = mealPlanService;
            _logger = logger;
        }

        // GET: Fridge/Index - Display fridge items
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var fridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
                var expiringItems = await _fridgeService.GetExpiringItemsAsync(accountId);
                
                var viewModel = new FridgeViewModel
                {
                    FridgeItems = fridgeItems.Select(MapToViewModel).ToList(),
                    ExpiringItems = expiringItems.Where(item => item.IsExpiring && !item.IsExpired).Select(MapToViewModel).ToList(),
                    ExpiredItems = expiringItems.Where(item => item.IsExpired).Select(MapToViewModel).ToList(),
                    TotalItems = fridgeItems.Count(),
                    ExpiringItemsCount = expiringItems.Count(item => item.IsExpiring && !item.IsExpired),
                    ExpiredItemsCount = expiringItems.Count(item => item.IsExpired)
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving fridge items for account {AccountId}", GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while loading your fridge items.";
                return View(new FridgeViewModel());
            }
        }

        // GET: Fridge/Add - Show add fridge item form
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            try
            {
                var ingredients = await _ingredientService.GetAllAsync();
                
                var viewModel = new AddFridgeItemViewModel
                {
                    ExpiryDate = DateTime.Today.AddDays(7),
                    AvailableIngredients = ingredients.Select(i => new IngredientSelectionViewModel
                    {
                        Id = i.Id,
                        IngredientName = i.IngredientName,
                        Unit = i.Unit,
                        IsSelected = false
                    }).ToList()
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading add fridge item form for account {AccountId}", GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while loading the form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Fridge/Add - Add fridge item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(AddFridgeItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload ingredients for the form
                var ingredients = await _ingredientService.GetAllAsync();
                model.AvailableIngredients = ingredients.Select(i => new IngredientSelectionViewModel
                {
                    Id = i.Id,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    IsSelected = i.Id == model.IngredientId
                }).ToList();
                
                return View(model);
            }

            try
            {
                var accountId = GetCurrentAccountId();
                var ingredient = await _ingredientService.GetByIdAsync(model.IngredientId);
                
                if (ingredient == null)
                {
                    ModelState.AddModelError(nameof(model.IngredientId), "Selected ingredient not found.");
                    
                    // Reload ingredients for the form
                    var ingredients = await _ingredientService.GetAllAsync();
                    model.AvailableIngredients = ingredients.Select(i => new IngredientSelectionViewModel
                    {
                        Id = i.Id,
                        IngredientName = i.IngredientName,
                        Unit = i.Unit,
                        IsSelected = i.Id == model.IngredientId
                    }).ToList();
                    
                    return View(model);
                }

                var fridgeItemDto = new FridgeItemDto
                {
                    AccountId = accountId,
                    IngredientId = model.IngredientId,
                    IngredientName = ingredient.IngredientName,
                    Unit = ingredient.Unit,
                    CurrentAmount = model.CurrentAmount,
                    ExpiryDate = model.ExpiryDate
                };

                await _fridgeService.AddItemAsync(fridgeItemDto);
                
                _logger.LogInformation("Fridge item {IngredientName} added successfully for account {AccountId}", 
                    ingredient.IngredientName, accountId);
                
                TempData["SuccessMessage"] = $"{ingredient.IngredientName} added to your fridge successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                
                // Reload ingredients for the form
                var ingredients = await _ingredientService.GetAllAsync();
                model.AvailableIngredients = ingredients.Select(i => new IngredientSelectionViewModel
                {
                    Id = i.Id,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    IsSelected = i.Id == model.IngredientId
                }).ToList();
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding fridge item for account {AccountId}", GetCurrentAccountId());
                ModelState.AddModelError(string.Empty, "An error occurred while adding the item. Please try again.");
                
                // Reload ingredients for the form
                var ingredients = await _ingredientService.GetAllAsync();
                model.AvailableIngredients = ingredients.Select(i => new IngredientSelectionViewModel
                {
                    Id = i.Id,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    IsSelected = i.Id == model.IngredientId
                }).ToList();
                
                return View(model);
            }
        }

        // POST: Fridge/UpdateItem - Update fridge item (quantity and/or expiry date)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItem(UpdateFridgeItemViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid input values.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var accountId = GetCurrentAccountId();
                var fridgeItem = await _fridgeService.GetFridgeItemsAsync(accountId);
                var item = fridgeItem.FirstOrDefault(f => f.Id == model.FridgeItemId);
                
                if (item == null)
                {
                    TempData["ErrorMessage"] = "Item not found.";
                    return RedirectToAction(nameof(Index));
                }

                bool quantityChanged = Math.Abs(item.CurrentAmount - model.NewAmount) > 0.001f;
                bool expiryChanged = item.ExpiryDate.Date != model.NewExpiryDate.Date;

                if (!quantityChanged && !expiryChanged)
                {
                    TempData["InfoMessage"] = "No changes were made.";
                    return RedirectToAction(nameof(Index));
                }

                // Update quantity if changed
                if (quantityChanged)
                {
                    await _fridgeService.UpdateItemQuantityAsync(model.FridgeItemId, model.NewAmount);
                    _logger.LogInformation("Fridge item {FridgeItemId} quantity updated from {OldAmount} to {NewAmount} for account {AccountId}", 
                        model.FridgeItemId, item.CurrentAmount, model.NewAmount, accountId);
                }

                // Update expiry date if changed
                if (expiryChanged)
                {
                    await _fridgeService.UpdateExpiryDateAsync(model.FridgeItemId, model.NewExpiryDate);
                    _logger.LogInformation("Fridge item {FridgeItemId} expiry date updated from {OldDate} to {NewDate} for account {AccountId}", 
                        model.FridgeItemId, item.ExpiryDate, model.NewExpiryDate, accountId);
                }

                // Build success message
                var changes = new List<string>();
                if (quantityChanged) changes.Add("quantity");
                if (expiryChanged) changes.Add("expiry date");
                
                var changeText = string.Join(" and ", changes);
                TempData["SuccessMessage"] = $"{model.IngredientName} {changeText} updated successfully!" + 
                    (expiryChanged ? " Items with matching expiry dates have been merged." : "");
                
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating fridge item {FridgeItemId} for account {AccountId}", 
                    model.FridgeItemId, GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while updating the item. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Fridge/UpdateQuantity - Update fridge item quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(UpdateQuantityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid quantity value.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _fridgeService.UpdateItemQuantityAsync(model.FridgeItemId, model.NewAmount);
                
                _logger.LogInformation("Fridge item {FridgeItemId} quantity updated to {NewAmount} for account {AccountId}", 
                    model.FridgeItemId, model.NewAmount, GetCurrentAccountId());
                
                TempData["SuccessMessage"] = $"{model.IngredientName} quantity updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating fridge item quantity {FridgeItemId} for account {AccountId}", 
                    model.FridgeItemId, GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while updating the quantity. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Fridge/UpdateExpiryDate - Update fridge item expiry date
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateExpiryDate(UpdateExpiryDateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid expiry date value.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _fridgeService.UpdateExpiryDateAsync(model.FridgeItemId, model.NewExpiryDate);
                
                _logger.LogInformation("Fridge item {FridgeItemId} expiry date updated to {NewExpiryDate} for account {AccountId}", 
                    model.FridgeItemId, model.NewExpiryDate, GetCurrentAccountId());
                
                TempData["SuccessMessage"] = $"{model.IngredientName} expiry date updated successfully! Duplicate items with the same expiry date have been merged.";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating fridge item expiry date {FridgeItemId} for account {AccountId}", 
                    model.FridgeItemId, GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while updating the expiry date. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Fridge/Remove - Remove fridge item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid id)
        {
            try
            {
                await _fridgeService.RemoveItemAsync(id);
                
                _logger.LogInformation("Fridge item {FridgeItemId} removed successfully for account {AccountId}", 
                    id, GetCurrentAccountId());
                
                TempData["SuccessMessage"] = "Item removed from your fridge successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing fridge item {FridgeItemId} for account {AccountId}", 
                    id, GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while removing the item. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Fridge/GroceryList - Show grocery list generation form or auto-generate from active plan
        [HttpGet]
        public async Task<IActionResult> GroceryList()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                
                // Try to get active meal plan
                var activePlan = await _mealPlanService.GetActivePlanAsync(accountId);
                
                if (activePlan != null)
                {
                    // Auto-generate grocery list from active plan
                    try
                    {
                        var groceryListDto = await _fridgeService.GenerateGroceryListFromActivePlanAsync(accountId);
                        
                        var viewModel = new GroceryListViewModel
                        {
                            AccountId = groceryListDto.AccountId,
                            MealPlanId = groceryListDto.MealPlanId,
                            MealPlanName = groceryListDto.MealPlanName,
                            GeneratedDate = groceryListDto.GeneratedDate,
                            MissingIngredients = groceryListDto.MissingIngredients.Select(gi => new GroceryItemViewModel
                            {
                                IngredientId = gi.IngredientId,
                                IngredientName = gi.IngredientName,
                                Unit = gi.Unit,
                                RequiredAmount = gi.RequiredAmount,
                                CurrentAmount = gi.CurrentAmount,
                                NeededAmount = gi.NeededAmount,
                                IsNeededSoon = gi.IsNeededSoon,
                                EarliestNeededDate = gi.EarliestNeededDate
                            }).ToList()
                        };
                        
                        _logger.LogInformation("Grocery list auto-generated from active plan for account {AccountId}", accountId);
                        
                        return View("GroceryListResult", viewModel);
                    }
                    catch (BusinessException ex)
                    {
                        TempData["ErrorMessage"] = ex.Message;
                    }
                }
                
                // No active plan or error - show meal plan selection form
                var mealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);
                
                var selectionViewModel = new GenerateGroceryListViewModel
                {
                    AvailableMealPlans = mealPlans.Select(mp => new MealPlanSelectionViewModel
                    {
                        Id = mp.Id,
                        PlanName = mp.PlanName,
                        StartDate = mp.StartDate,
                        EndDate = mp.EndDate,
                        IsAiGenerated = mp.IsAiGenerated,
                        IsActive = mp.IsActive,
                        IsSelected = false
                    }).ToList()
                };
                
                if (!mealPlans.Any())
                {
                    TempData["InfoMessage"] = "You don't have any meal plans yet. Create a meal plan first to generate a grocery list.";
                }
                else if (activePlan == null)
                {
                    TempData["InfoMessage"] = "No active meal plan set. Please select a meal plan below or set one as active from your meal plans.";
                }
                
                return View(selectionViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading grocery list for account {AccountId}", GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while loading the grocery list.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Fridge/GroceryList - Generate grocery list
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GroceryList(GenerateGroceryListViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload meal plans for the form
                var accountId = GetCurrentAccountId();
                var mealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);
                model.AvailableMealPlans = mealPlans.Select(mp => new MealPlanSelectionViewModel
                {
                    Id = mp.Id,
                    PlanName = mp.PlanName,
                    StartDate = mp.StartDate,
                    EndDate = mp.EndDate,
                    IsAiGenerated = mp.IsAiGenerated,
                    IsSelected = mp.Id == model.MealPlanId
                }).ToList();
                
                return View(model);
            }

            try
            {
                var accountId = GetCurrentAccountId();
                var groceryListDto = await _fridgeService.GenerateGroceryListAsync(accountId, model.MealPlanId);
                
                var viewModel = new GroceryListViewModel
                {
                    AccountId = groceryListDto.AccountId,
                    MealPlanId = groceryListDto.MealPlanId,
                    MealPlanName = groceryListDto.MealPlanName,
                    GeneratedDate = groceryListDto.GeneratedDate,
                    MissingIngredients = groceryListDto.MissingIngredients.Select(gi => new GroceryItemViewModel
                    {
                        IngredientId = gi.IngredientId,
                        IngredientName = gi.IngredientName,
                        Unit = gi.Unit,
                        RequiredAmount = gi.RequiredAmount,
                        CurrentAmount = gi.CurrentAmount,
                        NeededAmount = gi.NeededAmount,
                        IsNeededSoon = gi.IsNeededSoon,
                        EarliestNeededDate = gi.EarliestNeededDate
                    }).ToList()
                };
                
                _logger.LogInformation("Grocery list generated successfully for account {AccountId} and meal plan {MealPlanId}", 
                    accountId, model.MealPlanId);
                
                return View("GroceryListResult", viewModel);
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                
                // Reload meal plans for the form
                var accountId = GetCurrentAccountId();
                var mealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);
                model.AvailableMealPlans = mealPlans.Select(mp => new MealPlanSelectionViewModel
                {
                    Id = mp.Id,
                    PlanName = mp.PlanName,
                    StartDate = mp.StartDate,
                    EndDate = mp.EndDate,
                    IsAiGenerated = mp.IsAiGenerated,
                    IsSelected = mp.Id == model.MealPlanId
                }).ToList();
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating grocery list for account {AccountId} and meal plan {MealPlanId}", 
                    GetCurrentAccountId(), model.MealPlanId);
                ModelState.AddModelError(string.Empty, "An error occurred while generating the grocery list. Please try again.");
                
                // Reload meal plans for the form
                var accountId = GetCurrentAccountId();
                var mealPlans = await _mealPlanService.GetByAccountIdAsync(accountId);
                model.AvailableMealPlans = mealPlans.Select(mp => new MealPlanSelectionViewModel
                {
                    Id = mp.Id,
                    PlanName = mp.PlanName,
                    StartDate = mp.StartDate,
                    EndDate = mp.EndDate,
                    IsAiGenerated = mp.IsAiGenerated,
                    IsSelected = mp.Id == model.MealPlanId
                }).ToList();
                
                return View(model);
            }
        }

        // GET: Fridge/Stats - Show fridge statistics
        [HttpGet]
        public async Task<IActionResult> Stats()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var fridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
                var expiringItems = await _fridgeService.GetExpiringItemsAsync(accountId);
                
                var viewModel = new FridgeStatsViewModel
                {
                    TotalItems = fridgeItems.Count(),
                    FreshItems = fridgeItems.Count(item => !item.IsExpiring && !item.IsExpired),
                    ExpiringItems = expiringItems.Count(item => item.IsExpiring && !item.IsExpired),
                    ExpiredItems = expiringItems.Count(item => item.IsExpired),
                    LastUpdated = DateTime.Now
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving fridge statistics for account {AccountId}", GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while loading fridge statistics.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Fridge/SyncFridge - Synchronize purchased items with fridge
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncFridge(SyncFridgeViewModel model)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                
                if (model.PurchasedIngredients == null || !model.PurchasedIngredients.Any())
                {
                    TempData["ErrorMessage"] = "No items were selected to purchase.";
                    return RedirectToAction(nameof(GroceryList));
                }

                // Get the purchased items
                var purchasedItems = model.PurchasedIngredients.Values
                    .Where(p => p.IsPurchased)
                    .ToList();

                if (!purchasedItems.Any())
                {
                    TempData["ErrorMessage"] = "No items were selected to purchase.";
                    return RedirectToAction(nameof(GroceryList));
                }

                // Get current fridge items
                var fridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
                // Group by both IngredientId and ExpiryDate to treat same ingredient with different expiry dates as separate items
                var fridgeInventory = fridgeItems
                    .GroupBy(f => new { f.IngredientId, ExpiryDate = f.ExpiryDate.Date })
                    .ToDictionary(g => g.Key, g => g.First());

                int itemsAdded = 0;
                int itemsUpdated = 0;

                foreach (var purchasedItem in purchasedItems)
                {
                    // Get ingredient details
                    var ingredient = await _ingredientService.GetByIdAsync(purchasedItem.IngredientId);
                    if (ingredient == null)
                    {
                        _logger.LogWarning("Ingredient {IngredientId} not found while syncing fridge", purchasedItem.IngredientId);
                        continue;
                    }

                    var expiryDate = purchasedItem.ExpiryDate != default(DateTime) 
                        ? purchasedItem.ExpiryDate 
                        : DateTime.Today.AddDays(7); // Fallback to default 7 days expiry
                    
                    // Check if item exists with the same ingredient AND expiry date
                    var inventoryKey = new { IngredientId = purchasedItem.IngredientId, ExpiryDate = expiryDate.Date };

                    if (fridgeInventory.ContainsKey(inventoryKey))
                    {
                        // Update existing fridge item with same expiry date
                        var existingItem = fridgeInventory[inventoryKey];
                        var newAmount = existingItem.CurrentAmount + purchasedItem.Amount;
                        await _fridgeService.UpdateItemQuantityAsync(existingItem.Id, newAmount);
                        itemsUpdated++;
                        
                        _logger.LogInformation("Updated fridge item {IngredientName} (Expiry: {ExpiryDate}) from {OldAmount} to {NewAmount} for account {AccountId}",
                            ingredient.IngredientName, expiryDate.ToShortDateString(), existingItem.CurrentAmount, newAmount, accountId);
                    }
                    else
                    {
                        // Add new fridge item with the expiry date from the form
                        var fridgeItemDto = new FridgeItemDto
                        {
                            AccountId = accountId,
                            IngredientId = purchasedItem.IngredientId,
                            IngredientName = ingredient.IngredientName,
                            Unit = ingredient.Unit,
                            CurrentAmount = purchasedItem.Amount,
                            ExpiryDate = expiryDate
                        };
                        
                        await _fridgeService.AddItemAsync(fridgeItemDto);
                        itemsAdded++;
                        
                        _logger.LogInformation("Added new fridge item {IngredientName} with amount {Amount} and expiry date {ExpiryDate} for account {AccountId}",
                            ingredient.IngredientName, purchasedItem.Amount, expiryDate, accountId);
                    }
                }

                var successMessage = $"Successfully updated your fridge! {itemsAdded} item(s) added";
                if (itemsUpdated > 0)
                {
                    successMessage += $", {itemsUpdated} item(s) updated";
                }
                successMessage += ".";
                
                TempData["SuccessMessage"] = successMessage;
                
                _logger.LogInformation("Fridge sync completed for account {AccountId}: {ItemsAdded} added, {ItemsUpdated} updated", 
                    accountId, itemsAdded, itemsUpdated);
                
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(GroceryList));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while syncing fridge for account {AccountId}", GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while updating your fridge. Please try again.";
                return RedirectToAction(nameof(GroceryList));
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

        private FridgeItemViewModel MapToViewModel(FridgeItemDto dto)
        {
            return new FridgeItemViewModel
            {
                Id = dto.Id,
                AccountId = dto.AccountId,
                IngredientId = dto.IngredientId,
                IngredientName = dto.IngredientName,
                Unit = dto.Unit,
                CurrentAmount = dto.CurrentAmount,
                ExpiryDate = dto.ExpiryDate,
                IsExpiring = dto.IsExpiring,
                IsExpired = dto.IsExpired
            };
        }

        #endregion
    }
}