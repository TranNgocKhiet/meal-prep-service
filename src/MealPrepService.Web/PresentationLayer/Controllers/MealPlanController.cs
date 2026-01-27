using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.PresentationLayer.ViewModels;
using MealPrepService.Web.PresentationLayer.Filters;
using MealPrepService.DataAccessLayer.Repositories;

namespace MealPrepService.Web.PresentationLayer.Controllers
{
    [Authorize(Roles = "Customer,Manager")]
    public class MealPlanController : Controller
    {
        private readonly IMealPlanService _mealPlanService;
        private readonly IRecipeService _recipeService;
        private readonly IFridgeService _fridgeService;
        private readonly ISystemConfigurationService _systemConfigService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<MealPlanController> _logger;

        public MealPlanController(
            IMealPlanService mealPlanService,
            IRecipeService recipeService,
            IFridgeService fridgeService,
            ISystemConfigurationService systemConfigService,
            IUnitOfWork unitOfWork,
            ILogger<MealPlanController> logger)
        {
            _mealPlanService = mealPlanService;
            _recipeService = recipeService;
            _fridgeService = fridgeService;
            _systemConfigService = systemConfigService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: MealPlan/Index - List meal plans
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var mealPlanDtos = await _mealPlanService.GetByAccountIdAsync(accountId);
                
                var viewModels = mealPlanDtos.Select(MapToViewModel).ToList();
                
                // Calculate nutrition totals for each meal plan
                foreach (var viewModel in viewModels)
                {
                    CalculateNutritionTotals(viewModel);
                }
                
                // Sort meal plans: Active plans first, then by start date descending
                viewModels = viewModels
                    .OrderByDescending(p => p.IsActive)
                    .ThenByDescending(p => p.StartDate)
                    .ToList();
                
                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving meal plans for account {AccountId}", GetCurrentAccountId());
                TempData["ErrorMessage"] = "An error occurred while loading your meal plans.";
                return View(new List<MealPlanViewModel>());
            }
        }

        // GET: MealPlan/Details/{id} - View meal plan details with nutrition display
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var mealPlanDto = await _mealPlanService.GetByIdAsync(id);
                
                if (mealPlanDto == null)
                {
                    return NotFound("Meal plan not found.");
                }

                // Check if user owns this meal plan or is a manager
                var accountId = GetCurrentAccountId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (mealPlanDto.AccountId != accountId && userRole != "Manager")
                {
                    return Forbid("You don't have permission to view this meal plan.");
                }

                var viewModel = MapToViewModel(mealPlanDto);
                CalculateNutritionTotals(viewModel);
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving meal plan {MealPlanId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the meal plan.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: MealPlan/Create - Show create meal plan form
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
            
            var viewModel = new CreateMealPlanViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(maxDays)
            };
            
            ViewBag.MaxMealPlanDays = maxDays;
            
            return View(viewModel);
        }

        // POST: MealPlan/Create - Create manual meal plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMealPlanViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
                ViewBag.MaxMealPlanDays = maxDays;
                return View(model);
            }

            try
            {
                var accountId = GetCurrentAccountId();
                
                // Validate max days
                var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
                var daysDifference = (model.EndDate - model.StartDate).Days + 1;
                
                if (daysDifference > maxDays)
                {
                    ModelState.AddModelError(string.Empty, $"Meal plan cannot exceed {maxDays} days. Current selection: {daysDifference} days.");
                    ViewBag.MaxMealPlanDays = maxDays;
                    return View(model);
                }
                
                var mealPlanDto = new MealPlanDto
                {
                    AccountId = accountId,
                    PlanName = model.PlanName,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    IsAiGenerated = false
                };

                var createdPlan = await _mealPlanService.CreateManualMealPlanAsync(mealPlanDto);
                
                _logger.LogInformation("Manual meal plan {PlanName} created successfully for account {AccountId}", 
                    model.PlanName, accountId);
                
                TempData["SuccessMessage"] = "Meal plan created successfully!";
                return RedirectToAction(nameof(Details), new { id = createdPlan.Id });
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
                ViewBag.MaxMealPlanDays = maxDays;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating meal plan for account {AccountId}", GetCurrentAccountId());
                ModelState.AddModelError(string.Empty, "An error occurred while creating the meal plan. Please try again.");
                var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
                ViewBag.MaxMealPlanDays = maxDays;
                return View(model);
            }
        }

        // POST: MealPlan/GenerateAI - Generate AI meal plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateAI(CreateMealPlanViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
                ViewBag.MaxMealPlanDays = maxDays;
                return View("Create", model);
            }

            try
            {
                var accountId = GetCurrentAccountId();
                
                // Validate max days
                var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
                var daysDifference = (model.EndDate - model.StartDate).Days + 1;
                
                if (daysDifference > maxDays)
                {
                    ModelState.AddModelError(string.Empty, $"Meal plan cannot exceed {maxDays} days. Current selection: {daysDifference} days.");
                    ViewBag.MaxMealPlanDays = maxDays;
                    return View("Create", model);
                }
                
                var aiGeneratedPlan = await _mealPlanService.GenerateAiMealPlanAsync(
                    accountId, 
                    model.StartDate, 
                    model.EndDate,
                    model.PlanName); // Pass the custom plan name
                
                _logger.LogInformation("AI meal plan '{PlanName}' generated successfully for account {AccountId}", 
                    model.PlanName, accountId);
                
                TempData["SuccessMessage"] = "AI meal plan generated successfully!";
                return RedirectToAction(nameof(Details), new { id = aiGeneratedPlan.Id });
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
                ViewBag.MaxMealPlanDays = maxDays;
                return View("Create", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating AI meal plan for account {AccountId}", GetCurrentAccountId());
                ModelState.AddModelError(string.Empty, "An error occurred while generating the AI meal plan. Please try again.");
                var maxDays = await _systemConfigService.GetMaxMealPlanDaysAsync();
                ViewBag.MaxMealPlanDays = maxDays;
                return View("Create", model);
            }
        }

        // GET: MealPlan/AddMeal/{planId} - Show add meal form
        [HttpGet]
        public async Task<IActionResult> AddMeal(Guid planId, string searchTerm = "", bool showAll = false, int page = 1)
        {
            try
            {
                const int pageSize = 30;
                
                var mealPlan = await _mealPlanService.GetByIdAsync(planId);
                
                if (mealPlan == null)
                {
                    return NotFound("Meal plan not found.");
                }

                // Check if user owns this meal plan or is a manager
                var accountId = GetCurrentAccountId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (mealPlan.AccountId != accountId && userRole != "Manager")
                {
                    return Forbid("You don't have permission to modify this meal plan.");
                }

                // Load recipes if search term is provided OR showAll is true
                var recipes = new List<RecipeDto>();
                if (!string.IsNullOrWhiteSpace(searchTerm) || showAll)
                {
                    var allRecipes = await _recipeService.GetAllWithIngredientsAsync();
                    
                    // Apply search filter only if search term is provided
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        recipes = allRecipes
                            .Where(r => r.RecipeName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                    else if (showAll)
                    {
                        recipes = allRecipes.ToList();
                    }
                }

                // Calculate pagination
                var totalItems = recipes.Count;
                var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
                
                var pagedRecipes = recipes
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                var viewModel = new AddMealToPlanViewModel
                {
                    PlanId = planId,
                    ServeDate = mealPlan.StartDate,
                    SearchTerm = searchTerm,
                    ShowAll = showAll,
                    CurrentPage = page,
                    TotalPages = totalPages,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    AvailableRecipes = pagedRecipes.Select(r => new RecipeSelectionViewModel
                    {
                        Id = r.Id,
                        RecipeName = r.RecipeName,
                        TotalCalories = r.TotalCalories,
                        ProteinG = r.ProteinG,
                        FatG = r.FatG,
                        CarbsG = r.CarbsG,
                        IsSelected = false,
                        Ingredients = r.Ingredients?.Select(i => new RecipeIngredientViewModel
                        {
                            IngredientId = i.IngredientId,
                            IngredientName = i.IngredientName,
                            Amount = i.Amount,
                            Unit = i.Unit
                        }).ToList() ?? new List<RecipeIngredientViewModel>()
                    }).ToList()
                };
                
                ViewBag.PlanName = mealPlan.PlanName;
                ViewBag.StartDate = mealPlan.StartDate;
                ViewBag.EndDate = mealPlan.EndDate;
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading add meal form for plan {PlanId}", planId);
                TempData["ErrorMessage"] = "An error occurred while loading the form.";
                return RedirectToAction(nameof(Details), new { id = planId });
            }
        }

        // POST: MealPlan/AddMeal - Add meal to plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeal(AddMealToPlanViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload recipes for the form
                var recipes = await _recipeService.GetAllWithIngredientsAsync();
                model.AvailableRecipes = recipes.Select(r => new RecipeSelectionViewModel
                {
                    Id = r.Id,
                    RecipeName = r.RecipeName,
                    TotalCalories = r.TotalCalories,
                    ProteinG = r.ProteinG,
                    FatG = r.FatG,
                    CarbsG = r.CarbsG,
                    IsSelected = model.SelectedRecipeIds.Contains(r.Id),
                    Ingredients = r.Ingredients?.Select(i => new RecipeIngredientViewModel
                    {
                        IngredientId = i.IngredientId,
                        IngredientName = i.IngredientName,
                        Amount = i.Amount,
                        Unit = i.Unit
                    }).ToList() ?? new List<RecipeIngredientViewModel>()
                }).ToList();
                
                return View(model);
            }

            try
            {
                var selectedRecipes = new List<RecipeDto>();
                foreach (var recipeId in model.SelectedRecipeIds)
                {
                    var recipe = await _recipeService.GetByIdAsync(recipeId);
                    if (recipe != null)
                    {
                        selectedRecipes.Add(recipe);
                    }
                }

                var mealDto = new MealDto
                {
                    PlanId = model.PlanId,
                    MealType = model.MealType,
                    ServeDate = model.ServeDate,
                    Recipes = selectedRecipes
                };

                // Use the standard method for now (ingredient customization can be added later)
                await _mealPlanService.AddMealToPlanAsync(model.PlanId, mealDto);
                
                _logger.LogInformation("Meal added successfully to plan {PlanId}", model.PlanId);
                
                TempData["SuccessMessage"] = "Meal added to plan successfully!";
                return RedirectToAction(nameof(Details), new { id = model.PlanId });
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                
                // Reload recipes for the form
                var recipes = await _recipeService.GetAllWithIngredientsAsync();
                model.AvailableRecipes = recipes.Select(r => new RecipeSelectionViewModel
                {
                    Id = r.Id,
                    RecipeName = r.RecipeName,
                    TotalCalories = r.TotalCalories,
                    ProteinG = r.ProteinG,
                    FatG = r.FatG,
                    CarbsG = r.CarbsG,
                    IsSelected = model.SelectedRecipeIds.Contains(r.Id),
                    Ingredients = r.Ingredients?.Select(i => new RecipeIngredientViewModel
                    {
                        IngredientId = i.IngredientId,
                        IngredientName = i.IngredientName,
                        Amount = i.Amount,
                        Unit = i.Unit
                    }).ToList() ?? new List<RecipeIngredientViewModel>()
                }).ToList();
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding meal to plan {PlanId}", model.PlanId);
                ModelState.AddModelError(string.Empty, "An error occurred while adding the meal. Please try again.");
                
                // Reload recipes for the form
                var recipes = await _recipeService.GetAllWithIngredientsAsync();
                model.AvailableRecipes = recipes.Select(r => new RecipeSelectionViewModel
                {
                    Id = r.Id,
                    RecipeName = r.RecipeName,
                    TotalCalories = r.TotalCalories,
                    ProteinG = r.ProteinG,
                    FatG = r.FatG,
                    CarbsG = r.CarbsG,
                    IsSelected = model.SelectedRecipeIds.Contains(r.Id),
                    Ingredients = r.Ingredients?.Select(i => new RecipeIngredientViewModel
                    {
                        IngredientId = i.IngredientId,
                        IngredientName = i.IngredientName,
                        Amount = i.Amount,
                        Unit = i.Unit
                    }).ToList() ?? new List<RecipeIngredientViewModel>()
                }).ToList();
                
                return View(model);
            }
        }

        // GET: MealPlan/Delete/{id} - Show delete confirmation
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var mealPlanDto = await _mealPlanService.GetByIdAsync(id);
                
                if (mealPlanDto == null)
                {
                    return NotFound("Meal plan not found.");
                }

                // Check authorization
                var accountId = GetCurrentAccountId();
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (mealPlanDto.AccountId != accountId && userRole != "Manager")
                {
                    return Forbid("You don't have permission to delete this meal plan.");
                }

                var viewModel = MapToViewModel(mealPlanDto);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading delete confirmation for meal plan {MealPlanId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the meal plan.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: MealPlan/DeleteConfirmed - Confirm deletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                await _mealPlanService.DeleteAsync(id, accountId);
                
                _logger.LogInformation("Meal plan {MealPlanId} deleted successfully by account {AccountId}", id, accountId);
                
                TempData["SuccessMessage"] = "Meal plan deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (NotFoundException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (AuthorizationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting meal plan {MealPlanId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the meal plan. Please try again.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: MealPlan/SetActive - Set meal plan as active
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetActive(Guid id)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                await _mealPlanService.SetActivePlanAsync(id, accountId);
                
                _logger.LogInformation("Meal plan {MealPlanId} set as active by account {AccountId}", id, accountId);
                
                TempData["SuccessMessage"] = "Meal plan set as active! Your grocery list will now be based on this plan.";
                return RedirectToAction(nameof(Index));
            }
            catch (NotFoundException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (AuthorizationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while setting meal plan {MealPlanId} as active", id);
                TempData["ErrorMessage"] = "An error occurred while setting the meal plan as active. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: MealPlan/RemoveRecipeFromMeal - Remove a recipe from a meal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRecipeFromMeal(Guid mealId, Guid recipeId, Guid planId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                await _mealPlanService.RemoveRecipeFromMealAsync(mealId, recipeId, accountId);
                
                _logger.LogInformation("Recipe {RecipeId} removed from meal {MealId} by account {AccountId}", 
                    recipeId, mealId, accountId);
                
                TempData["SuccessMessage"] = "Recipe removed from meal successfully!";
                return RedirectToAction(nameof(Details), new { id = planId });
            }
            catch (NotFoundException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = planId });
            }
            catch (AuthorizationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = planId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while removing recipe {RecipeId} from meal {MealId}", 
                    recipeId, mealId);
                TempData["ErrorMessage"] = "An error occurred while removing the recipe. Please try again.";
                return RedirectToAction(nameof(Details), new { id = planId });
            }
        }

        // GET: MealPlan/CheckMealIngredients - Check if ingredients are available for a meal
        [HttpGet]
        public async Task<IActionResult> CheckMealIngredients(Guid mealId, Guid planId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var mealPlan = await _mealPlanService.GetByIdAsync(planId);
                
                if (mealPlan == null || mealPlan.AccountId != accountId)
                {
                    return Json(new { success = false, message = "Meal plan not found or access denied." });
                }

                var meal = mealPlan.Meals.FirstOrDefault(m => m.Id == mealId);
                if (meal == null)
                {
                    return Json(new { success = false, message = "Meal not found." });
                }

                // Get fridge items - group by ingredient ID and sum amounts to handle multiple items of same ingredient
                var fridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
                var fridgeInventory = fridgeItems
                    .GroupBy(f => f.IngredientId)
                    .ToDictionary(
                        g => g.Key, 
                        g => new { 
                            Items = g.ToList(), 
                            TotalAmount = g.Sum(f => f.CurrentAmount) 
                        });

                // Calculate required ingredients
                var requiredIngredients = new Dictionary<Guid, float>();
                var ingredientNames = new Dictionary<Guid, string>();
                var ingredientUnits = new Dictionary<Guid, string>();

                foreach (var recipe in meal.Recipes)
                {
                    if (recipe.Ingredients != null)
                    {
                        foreach (var ingredient in recipe.Ingredients)
                        {
                            if (requiredIngredients.ContainsKey(ingredient.IngredientId))
                            {
                                requiredIngredients[ingredient.IngredientId] += ingredient.Amount;
                            }
                            else
                            {
                                requiredIngredients[ingredient.IngredientId] = ingredient.Amount;
                                ingredientNames[ingredient.IngredientId] = ingredient.IngredientName;
                                ingredientUnits[ingredient.IngredientId] = ingredient.Unit;
                            }
                        }
                    }
                }

                // Check availability
                var missingIngredients = new List<object>();
                var insufficientIngredients = new List<object>();
                var consumptionPlan = new List<object>();

                foreach (var required in requiredIngredients)
                {
                    var ingredientId = required.Key;
                    var requiredAmount = required.Value;
                    var ingredientName = ingredientNames[ingredientId];
                    var unit = ingredientUnits[ingredientId];

                    if (!fridgeInventory.ContainsKey(ingredientId))
                    {
                        missingIngredients.Add(new
                        {
                            name = ingredientName,
                            required = requiredAmount,
                            unit = unit
                        });
                    }
                    else
                    {
                        var fridgeGroup = fridgeInventory[ingredientId];
                        if (fridgeGroup.TotalAmount < requiredAmount)
                        {
                            insufficientIngredients.Add(new
                            {
                                name = ingredientName,
                                required = requiredAmount,
                                available = fridgeGroup.TotalAmount,
                                unit = unit
                            });
                        }
                        else
                        {
                            // Show which fridge items will be consumed (ordered by expiry date)
                            var itemsToConsume = fridgeGroup.Items.OrderBy(f => f.ExpiryDate).ToList();
                            var remainingToConsume = requiredAmount;
                            
                            foreach (var item in itemsToConsume)
                            {
                                if (remainingToConsume <= 0) break;
                                
                                var amountToConsume = Math.Min(item.CurrentAmount, remainingToConsume);
                                var newAmount = item.CurrentAmount - amountToConsume;
                                
                                consumptionPlan.Add(new
                                {
                                    fridgeItemId = item.Id,
                                    ingredientName = ingredientName,
                                    amount = amountToConsume,
                                    newAmount = newAmount,
                                    unit = unit,
                                    expiryDate = item.ExpiryDate
                                });
                                
                                remainingToConsume -= amountToConsume;
                            }
                        }
                    }
                }

                return Json(new
                {
                    success = true,
                    hasIssues = missingIngredients.Any() || insufficientIngredients.Any(),
                    missingIngredients = missingIngredients,
                    insufficientIngredients = insufficientIngredients,
                    consumptionPlan = consumptionPlan
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking meal ingredients for meal {MealId}", mealId);
                return Json(new { success = false, message = "An error occurred while checking ingredients." });
            }
        }

        // POST: MealPlan/FinishMeal - Consume ingredients from fridge for a meal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinishMeal(Guid mealId, Guid planId, bool forceComplete = false)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var mealPlan = await _mealPlanService.GetByIdAsync(planId);
                
                if (mealPlan == null || mealPlan.AccountId != accountId)
                {
                    TempData["ErrorMessage"] = "Meal plan not found or access denied.";
                    return RedirectToAction(nameof(Details), new { id = planId });
                }

                var meal = mealPlan.Meals.FirstOrDefault(m => m.Id == mealId);
                if (meal == null)
                {
                    TempData["ErrorMessage"] = "Meal not found.";
                    return RedirectToAction(nameof(Details), new { id = planId });
                }

                // Check if meal is already finished
                if (meal.MealFinished)
                {
                    TempData["InfoMessage"] = "This meal has already been finished.";
                    return RedirectToAction(nameof(Details), new { id = planId });
                }

                // Get fridge items - sorted by expiry date (closest first)
                var fridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
                var fridgeItemsList = fridgeItems.OrderBy(f => f.ExpiryDate).ToList();

                // Calculate required ingredients
                var requiredIngredients = new Dictionary<Guid, float>();
                foreach (var recipe in meal.Recipes)
                {
                    if (recipe.Ingredients != null)
                    {
                        foreach (var ingredient in recipe.Ingredients)
                        {
                            if (requiredIngredients.ContainsKey(ingredient.IngredientId))
                            {
                                requiredIngredients[ingredient.IngredientId] += ingredient.Amount;
                            }
                            else
                            {
                                requiredIngredients[ingredient.IngredientId] = ingredient.Amount;
                            }
                        }
                    }
                }

                // Consume ingredients - prioritize items with closest expiry date
                var updatedCount = 0;
                var removedCount = 0;
                foreach (var required in requiredIngredients)
                {
                    var ingredientId = required.Key;
                    var requiredAmount = required.Value;

                    // Get all fridge items for this ingredient, ordered by expiry date (closest first)
                    var availableItems = fridgeItemsList
                        .Where(f => f.IngredientId == ingredientId)
                        .OrderBy(f => f.ExpiryDate)
                        .ToList();

                    var remainingRequired = requiredAmount;

                    foreach (var fridgeItem in availableItems)
                    {
                        if (remainingRequired <= 0) break;

                        var amountToConsume = Math.Min(fridgeItem.CurrentAmount, remainingRequired);
                        var newAmount = fridgeItem.CurrentAmount - amountToConsume;
                        remainingRequired -= amountToConsume;

                        if (newAmount <= 0.001f) // Use small epsilon for floating point comparison
                        {
                            // Remove item from fridge when quantity reaches 0
                            await _fridgeService.RemoveItemAsync(fridgeItem.Id);
                            removedCount++;
                        }
                        else
                        {
                            // Update quantity if still > 0
                            await _fridgeService.UpdateItemQuantityAsync(fridgeItem.Id, newAmount);
                        }
                        updatedCount++;
                    }
                }

                // Mark meal as finished in database
                await _mealPlanService.MarkMealAsFinishedAsync(mealId, accountId, true);

                _logger.LogInformation("Meal {MealId} marked as finished. {Count} fridge items updated ({Removed} removed) for account {AccountId}", 
                    mealId, updatedCount, removedCount, accountId);
                
                var successMessage = $"Meal finished! {updatedCount} ingredients consumed from your fridge";
                if (removedCount > 0)
                {
                    successMessage += $" ({removedCount} item{(removedCount > 1 ? "s" : "")} completely used up and removed)";
                }
                successMessage += ".";
                
                TempData["SuccessMessage"] = successMessage;
                return RedirectToAction(nameof(Details), new { id = planId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finishing meal {MealId}", mealId);
                TempData["ErrorMessage"] = "An error occurred while finishing the meal. Please try again.";
                return RedirectToAction(nameof(Details), new { id = planId });
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

        private MealPlanViewModel MapToViewModel(MealPlanDto dto)
        {
            var viewModel = new MealPlanViewModel
            {
                Id = dto.Id,
                AccountId = dto.AccountId,
                PlanName = dto.PlanName,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsAiGenerated = dto.IsAiGenerated,
                IsActive = dto.IsActive,
                Meals = dto.Meals.Select(MapMealToViewModel).ToList()
            };

            return viewModel;
        }

        private MealViewModel MapMealToViewModel(MealDto dto)
        {
            var viewModel = new MealViewModel
            {
                Id = dto.Id,
                PlanId = dto.PlanId,
                MealType = dto.MealType,
                ServeDate = dto.ServeDate,
                MealFinished = dto.MealFinished,
                Recipes = dto.Recipes.Select(MapRecipeToViewModel).ToList()
            };

            // Calculate meal nutrition totals from recipes
            viewModel.MealCalories = viewModel.Recipes.Sum(r => r.TotalCalories);
            viewModel.MealProteinG = viewModel.Recipes.Sum(r => r.ProteinG);
            viewModel.MealFatG = viewModel.Recipes.Sum(r => r.FatG);
            viewModel.MealCarbsG = viewModel.Recipes.Sum(r => r.CarbsG);

            return viewModel;
        }

        private RecipeViewModel MapRecipeToViewModel(RecipeDto dto)
        {
            return new RecipeViewModel
            {
                Id = dto.Id,
                RecipeName = dto.RecipeName,
                Instructions = dto.Instructions,
                TotalCalories = dto.TotalCalories,
                ProteinG = dto.ProteinG,
                FatG = dto.FatG,
                CarbsG = dto.CarbsG,
                Ingredients = dto.Ingredients?.Select(i => new RecipeIngredientViewModel
                {
                    IngredientId = i.IngredientId,
                    IngredientName = i.IngredientName,
                    Amount = i.Amount,
                    Unit = i.Unit
                }).ToList() ?? new List<RecipeIngredientViewModel>()
            };
        }

        private void CalculateNutritionTotals(MealPlanViewModel viewModel)
        {
            // Calculate total nutrition for the entire plan
            viewModel.TotalCalories = viewModel.Meals.Sum(m => m.MealCalories);
            viewModel.TotalProteinG = viewModel.Meals.Sum(m => m.MealProteinG);
            viewModel.TotalFatG = viewModel.Meals.Sum(m => m.MealFatG);
            viewModel.TotalCarbsG = viewModel.Meals.Sum(m => m.MealCarbsG);

            // Calculate finished nutrition (from completed meals only)
            var finishedMeals = viewModel.Meals.Where(m => m.MealFinished).ToList();
            viewModel.FinishedCalories = finishedMeals.Sum(m => m.MealCalories);
            viewModel.FinishedProteinG = finishedMeals.Sum(m => m.MealProteinG);
            viewModel.FinishedFatG = finishedMeals.Sum(m => m.MealFatG);
            viewModel.FinishedCarbsG = finishedMeals.Sum(m => m.MealCarbsG);
            viewModel.FinishedMealCount = finishedMeals.Count;

            // Calculate daily nutrition breakdown
            var dailyNutrition = new Dictionary<DateTime, DailyNutritionViewModel>();
            
            foreach (var meal in viewModel.Meals)
            {
                var date = meal.ServeDate.Date;
                
                if (!dailyNutrition.ContainsKey(date))
                {
                    dailyNutrition[date] = new DailyNutritionViewModel
                    {
                        Date = date,
                        Meals = new List<MealViewModel>()
                    };
                }

                dailyNutrition[date].Meals.Add(meal);
                dailyNutrition[date].DailyCalories += meal.MealCalories;
                dailyNutrition[date].DailyProteinG += meal.MealProteinG;
                dailyNutrition[date].DailyFatG += meal.MealFatG;
                dailyNutrition[date].DailyCarbsG += meal.MealCarbsG;
            }

            // Sort meals within each day by meal type order (Breakfast, Lunch, Dinner)
            foreach (var day in dailyNutrition.Values)
            {
                day.Meals = day.Meals.OrderBy(m => MealViewModel.GetMealTypeOrder(m.MealType)).ToList();
            }

            viewModel.DailyNutrition = dailyNutrition;
        }

        #endregion

        #region Mark Meal as Finished

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MarkMealFinished(Guid mealId, Guid planId, bool finished)
        {
            try
            {
                var accountId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "");
                
                // If unmarking a finished meal, restore ingredients to the fridge
                if (!finished)
                {
                    await RestoreIngredientsToFridge(mealId, planId, accountId);
                }
                
                await _mealPlanService.MarkMealAsFinishedAsync(mealId, accountId, finished);
                
                TempData["SuccessMessage"] = finished ? "Meal marked as finished!" : "Meal marked as not finished and ingredients restored to fridge.";
                return RedirectToAction(nameof(Details), new { id = planId });
            }
            catch (NotFoundException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = planId });
            }
            catch (AuthorizationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = planId });
            }
            catch (BusinessException ex)
            {
                _logger.LogError(ex, "Business error marking meal as finished");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = planId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking meal as finished");
                TempData["ErrorMessage"] = "An error occurred while updating the meal status.";
                return RedirectToAction(nameof(Details), new { id = planId });
            }
        }

        private async Task RestoreIngredientsToFridge(Guid mealId, Guid planId, Guid accountId)
        {
            try
            {
                // Get the meal plan and meal details
                var mealPlan = await _mealPlanService.GetByIdAsync(planId);
                if (mealPlan == null || mealPlan.AccountId != accountId)
                {
                    throw new AuthorizationException("Access denied to this meal plan.");
                }

                var meal = mealPlan.Meals.FirstOrDefault(m => m.Id == mealId);
                if (meal == null)
                {
                    throw new NotFoundException("Meal not found.");
                }

                // Calculate ingredients to restore
                var ingredientsToRestore = new Dictionary<Guid, (string Name, float Amount, string Unit)>();
                foreach (var recipe in meal.Recipes)
                {
                    if (recipe.Ingredients != null)
                    {
                        foreach (var ingredient in recipe.Ingredients)
                        {
                            if (ingredientsToRestore.ContainsKey(ingredient.IngredientId))
                            {
                                var existing = ingredientsToRestore[ingredient.IngredientId];
                                ingredientsToRestore[ingredient.IngredientId] = (
                                    existing.Name,
                                    existing.Amount + ingredient.Amount,
                                    existing.Unit
                                );
                            }
                            else
                            {
                                ingredientsToRestore[ingredient.IngredientId] = (
                                    ingredient.IngredientName,
                                    ingredient.Amount,
                                    ingredient.Unit
                                );
                            }
                        }
                    }
                }

                // Get current fridge items
                var fridgeItems = await _fridgeService.GetFridgeItemsAsync(accountId);
                // Group by IngredientId and take the first item for each ingredient (in case of duplicates)
                var fridgeInventory = fridgeItems
                    .GroupBy(f => f.IngredientId)
                    .ToDictionary(g => g.Key, g => g.First());

                int restoredCount = 0;
                int addedCount = 0;

                // Restore ingredients to fridge
                foreach (var ingredient in ingredientsToRestore)
                {
                    var ingredientId = ingredient.Key;
                    var (name, amount, unit) = ingredient.Value;

                    if (fridgeInventory.ContainsKey(ingredientId))
                    {
                        // Update existing fridge item
                        var fridgeItem = fridgeInventory[ingredientId];
                        var newAmount = fridgeItem.CurrentAmount + amount;
                        await _fridgeService.UpdateItemQuantityAsync(fridgeItem.Id, newAmount);
                        restoredCount++;
                    }
                    else
                    {
                        // Add new fridge item (ingredient was completely consumed before)
                        // Set a default expiry date of 7 days from now when restoring ingredients
                        await _fridgeService.AddItemAsync(new FridgeItemDto
                        {
                            AccountId = accountId,
                            IngredientId = ingredientId,
                            CurrentAmount = amount,
                            ExpiryDate = DateTime.UtcNow.Date.AddDays(7)
                        });
                        addedCount++;
                    }
                }

                _logger.LogInformation(
                    "Restored ingredients for meal {MealId}: {RestoredCount} updated, {AddedCount} added back to fridge for account {AccountId}",
                    mealId, restoredCount, addedCount, accountId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restoring ingredients for meal {MealId}", mealId);
                throw;
            }
        }

        #endregion
    }
}