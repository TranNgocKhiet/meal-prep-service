using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.PresentationLayer.ViewModels;
using MealPrepService.Web.PresentationLayer.Filters;

namespace MealPrepService.Web.PresentationLayer.Controllers
{
    [Authorize(Roles = "Customer,Manager")]
    public class MealPlanController : Controller
    {
        private readonly IMealPlanService _mealPlanService;
        private readonly IRecipeService _recipeService;
        private readonly ILogger<MealPlanController> _logger;

        public MealPlanController(
            IMealPlanService mealPlanService,
            IRecipeService recipeService,
            ILogger<MealPlanController> logger)
        {
            _mealPlanService = mealPlanService;
            _recipeService = recipeService;
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
        public IActionResult Create()
        {
            var viewModel = new CreateMealPlanViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7)
            };
            
            return View(viewModel);
        }

        // POST: MealPlan/Create - Create manual meal plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMealPlanViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var accountId = GetCurrentAccountId();
                
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
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating meal plan for account {AccountId}", GetCurrentAccountId());
                ModelState.AddModelError(string.Empty, "An error occurred while creating the meal plan. Please try again.");
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
                return View("Create", model);
            }

            try
            {
                var accountId = GetCurrentAccountId();
                
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
                return View("Create", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating AI meal plan for account {AccountId}", GetCurrentAccountId());
                ModelState.AddModelError(string.Empty, "An error occurred while generating the AI meal plan. Please try again.");
                return View("Create", model);
            }
        }

        // GET: MealPlan/AddMeal/{planId} - Show add meal form
        [HttpGet]
        public async Task<IActionResult> AddMeal(Guid planId)
        {
            try
            {
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

                var recipes = await _recipeService.GetAllWithIngredientsAsync();
                
                var viewModel = new AddMealToPlanViewModel
                {
                    PlanId = planId,
                    ServeDate = mealPlan.StartDate,
                    AvailableRecipes = recipes.Select(r => new RecipeSelectionViewModel
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
                
                await _mealPlanService.MarkMealAsFinishedAsync(mealId, accountId, finished);
                
                TempData["SuccessMessage"] = finished ? "Meal marked as finished!" : "Meal marked as not finished.";
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
                _logger.LogError(ex, "Error marking meal as finished");
                TempData["ErrorMessage"] = "An error occurred while updating the meal status.";
                return RedirectToAction(nameof(Details), new { id = planId });
            }
        }

        #endregion
    }
}