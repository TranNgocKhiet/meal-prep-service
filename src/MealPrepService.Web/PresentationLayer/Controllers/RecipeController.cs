using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.PresentationLayer.ViewModels;

namespace MealPrepService.Web.PresentationLayer.Controllers
{
    /// <summary>
    /// Controller for recipe management operations (Admin and Manager roles)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    public class RecipeController : Controller
    {
        private readonly IRecipeService _recipeService;
        private readonly IIngredientService _ingredientService;
        private readonly ILogger<RecipeController> _logger;

        public RecipeController(
            IRecipeService recipeService,
            IIngredientService ingredientService,
            ILogger<RecipeController> logger)
        {
            _recipeService = recipeService;
            _ingredientService = ingredientService;
            _logger = logger;
        }

        // GET: Recipe/Index - List all recipes
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm = "", bool showOnlyWithIngredients = false)
        {
            try
            {
                var recipeDtos = await _recipeService.GetAllAsync();
                var recipes = recipeDtos.Select(MapToViewModel).ToList();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    recipes = recipes.Where(r => 
                        r.RecipeName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        r.Instructions.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (showOnlyWithIngredients)
                {
                    recipes = recipes.Where(r => r.Ingredients.Any()).ToList();
                }

                var viewModel = new RecipeListViewModel
                {
                    Recipes = recipes,
                    SearchTerm = searchTerm,
                    ShowOnlyWithIngredients = showOnlyWithIngredients
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving recipes");
                TempData["ErrorMessage"] = "An error occurred while loading the recipes.";
                return View(new RecipeListViewModel());
            }
        }

        // GET: Recipe/Details/{id} - View recipe details
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var recipeDto = await _recipeService.GetByIdAsync(id);
                
                if (recipeDto == null)
                {
                    return NotFound("Recipe not found.");
                }

                var viewModel = MapToViewModel(recipeDto);
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving recipe {RecipeId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the recipe.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Recipe/Create - Show create recipe form
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateRecipeViewModel());
        }

        // POST: Recipe/Create - Create a new recipe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRecipeViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            try
            {
                var createDto = new CreateRecipeDto
                {
                    RecipeName = viewModel.RecipeName,
                    Instructions = viewModel.Instructions
                };

                var createdRecipe = await _recipeService.CreateRecipeAsync(createDto);

                TempData["SuccessMessage"] = $"Recipe '{createdRecipe.RecipeName}' created successfully.";
                return RedirectToAction(nameof(Details), new { id = createdRecipe.Id });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating recipe");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(viewModel);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Business error while creating recipe");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating recipe");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the recipe.");
                return View(viewModel);
            }
        }

        // GET: Recipe/Edit/{id} - Show edit recipe form
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var recipeDto = await _recipeService.GetByIdAsync(id);
                
                if (recipeDto == null)
                {
                    return NotFound("Recipe not found.");
                }

                var viewModel = new EditRecipeViewModel
                {
                    Id = recipeDto.Id,
                    RecipeName = recipeDto.RecipeName,
                    Instructions = recipeDto.Instructions,
                    TotalCalories = recipeDto.TotalCalories,
                    ProteinG = recipeDto.ProteinG,
                    FatG = recipeDto.FatG,
                    CarbsG = recipeDto.CarbsG,
                    Ingredients = new List<RecipeIngredientViewModel>() // TODO: Map ingredients when available
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading recipe {RecipeId} for editing", id);
                TempData["ErrorMessage"] = "An error occurred while loading the recipe.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Recipe/Edit/{id} - Update an existing recipe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditRecipeViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return BadRequest("Recipe ID mismatch.");
            }

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            try
            {
                var updateDto = new UpdateRecipeDto
                {
                    RecipeName = viewModel.RecipeName,
                    Instructions = viewModel.Instructions
                };

                var updatedRecipe = await _recipeService.UpdateRecipeAsync(id, updateDto);

                TempData["SuccessMessage"] = $"Recipe '{updatedRecipe.RecipeName}' updated successfully.";
                return RedirectToAction(nameof(Details), new { id = updatedRecipe.Id });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Recipe {RecipeId} not found", id);
                return NotFound(ex.Message);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating recipe {RecipeId}", id);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(viewModel);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Business error while updating recipe {RecipeId}", id);
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating recipe {RecipeId}", id);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the recipe.");
                return View(viewModel);
            }
        }

        // POST: Recipe/Delete/{id} - Delete a recipe with constraint check
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _recipeService.DeleteRecipeAsync(id);

                TempData["SuccessMessage"] = "Recipe deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Recipe {RecipeId} not found", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (ConstraintViolationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete recipe {RecipeId} due to constraint violation", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Business error while deleting recipe {RecipeId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting recipe {RecipeId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the recipe.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // GET: Recipe/AddIngredient/{id} - Show add ingredient form
        [HttpGet]
        public async Task<IActionResult> AddIngredient(Guid id)
        {
            try
            {
                var recipeDto = await _recipeService.GetByIdAsync(id);
                
                if (recipeDto == null)
                {
                    return NotFound("Recipe not found.");
                }

                // Get all available ingredients
                var ingredientDtos = await _ingredientService.GetAllAsync();
                var availableIngredients = ingredientDtos.Select(i => new RecipeIngredientSelectionViewModel
                {
                    Id = i.Id,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    CaloPerUnit = i.CaloPerUnit,
                    IsAllergen = i.IsAllergen,
                    IsSelected = false
                }).ToList();

                var viewModel = new AddIngredientToRecipeViewModel
                {
                    RecipeId = id,
                    RecipeName = recipeDto.RecipeName,
                    AvailableIngredients = availableIngredients
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading add ingredient form for recipe {RecipeId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the form.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        // POST: Recipe/AddIngredient - Add an ingredient to a recipe
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddIngredient(AddIngredientToRecipeViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                // Reload available ingredients
                var ingredientDtos = await _ingredientService.GetAllAsync();
                viewModel.AvailableIngredients = ingredientDtos.Select(i => new RecipeIngredientSelectionViewModel
                {
                    Id = i.Id,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    CaloPerUnit = i.CaloPerUnit,
                    IsAllergen = i.IsAllergen,
                    IsSelected = i.Id == viewModel.IngredientId
                }).ToList();

                return View(viewModel);
            }

            try
            {
                var ingredientDto = new RecipeIngredientDto
                {
                    IngredientId = viewModel.IngredientId,
                    Amount = viewModel.Amount
                };

                await _recipeService.AddIngredientToRecipeAsync(viewModel.RecipeId, ingredientDto);

                TempData["SuccessMessage"] = "Ingredient added to recipe successfully.";
                return RedirectToAction(nameof(Details), new { id = viewModel.RecipeId });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Recipe or ingredient not found");
                ModelState.AddModelError(string.Empty, ex.Message);
                
                // Reload available ingredients
                var ingredientDtos = await _ingredientService.GetAllAsync();
                viewModel.AvailableIngredients = ingredientDtos.Select(i => new RecipeIngredientSelectionViewModel
                {
                    Id = i.Id,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    CaloPerUnit = i.CaloPerUnit,
                    IsAllergen = i.IsAllergen,
                    IsSelected = i.Id == viewModel.IngredientId
                }).ToList();

                return View(viewModel);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while adding ingredient to recipe");
                ModelState.AddModelError(string.Empty, ex.Message);
                
                // Reload available ingredients
                var ingredientDtos = await _ingredientService.GetAllAsync();
                viewModel.AvailableIngredients = ingredientDtos.Select(i => new RecipeIngredientSelectionViewModel
                {
                    Id = i.Id,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    CaloPerUnit = i.CaloPerUnit,
                    IsAllergen = i.IsAllergen,
                    IsSelected = i.Id == viewModel.IngredientId
                }).ToList();

                return View(viewModel);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Business error while adding ingredient to recipe");
                ModelState.AddModelError(string.Empty, ex.Message);
                
                // Reload available ingredients
                var ingredientDtos = await _ingredientService.GetAllAsync();
                viewModel.AvailableIngredients = ingredientDtos.Select(i => new RecipeIngredientSelectionViewModel
                {
                    Id = i.Id,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    CaloPerUnit = i.CaloPerUnit,
                    IsAllergen = i.IsAllergen,
                    IsSelected = i.Id == viewModel.IngredientId
                }).ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding ingredient to recipe {RecipeId}", viewModel.RecipeId);
                ModelState.AddModelError(string.Empty, "An error occurred while adding the ingredient.");
                
                // Reload available ingredients
                var ingredientDtos = await _ingredientService.GetAllAsync();
                viewModel.AvailableIngredients = ingredientDtos.Select(i => new RecipeIngredientSelectionViewModel
                {
                    Id = i.Id,
                    IngredientName = i.IngredientName,
                    Unit = i.Unit,
                    CaloPerUnit = i.CaloPerUnit,
                    IsAllergen = i.IsAllergen,
                    IsSelected = i.Id == viewModel.IngredientId
                }).ToList();

                return View(viewModel);
            }
        }

        #region Helper Methods

        /// <summary>
        /// Maps RecipeDto to RecipeViewModel
        /// </summary>
        private RecipeViewModel MapToViewModel(RecipeDto dto)
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
                Ingredients = new List<RecipeIngredientViewModel>() // TODO: Map ingredients when available
            };
        }

        #endregion
    }
}
