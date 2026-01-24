using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.Web.PresentationLayer.ViewModels;

namespace MealPrepService.Web.PresentationLayer.Controllers
{
    /// <summary>
    /// Controller for ingredient management operations (Admin and Manager roles)
    /// </summary>
    [Authorize(Roles = "Admin,Manager")]
    public class IngredientController : Controller
    {
        private readonly IIngredientService _ingredientService;
        private readonly ILogger<IngredientController> _logger;

        public IngredientController(
            IIngredientService ingredientService,
            ILogger<IngredientController> logger)
        {
            _ingredientService = ingredientService;
            _logger = logger;
        }

        // GET: Ingredient/Index - List all ingredients
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm = "", bool? showOnlyAllergens = null)
        {
            try
            {
                var ingredientDtos = await _ingredientService.GetAllAsync();
                var ingredients = ingredientDtos.Select(MapToViewModel).ToList();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    ingredients = ingredients.Where(i => 
                        i.IngredientName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        i.Unit.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (showOnlyAllergens.HasValue)
                {
                    ingredients = ingredients.Where(i => i.IsAllergen == showOnlyAllergens.Value).ToList();
                }

                var viewModel = new IngredientListViewModel
                {
                    Ingredients = ingredients,
                    SearchTerm = searchTerm,
                    ShowOnlyAllergens = showOnlyAllergens
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving ingredients");
                TempData["ErrorMessage"] = "An error occurred while loading the ingredients.";
                return View(new IngredientListViewModel());
            }
        }

        // GET: Ingredient/Details/{id} - View ingredient details
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var ingredientDto = await _ingredientService.GetByIdAsync(id);
                
                if (ingredientDto == null)
                {
                    return NotFound("Ingredient not found.");
                }

                var viewModel = MapToViewModel(ingredientDto);
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving ingredient {IngredientId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the ingredient.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Ingredient/Create - Show create ingredient form
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateIngredientViewModel());
        }

        // POST: Ingredient/Create - Create a new ingredient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateIngredientViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            try
            {
                var createDto = new CreateIngredientDto
                {
                    IngredientName = viewModel.IngredientName,
                    Unit = viewModel.Unit,
                    CaloPerUnit = viewModel.CaloPerUnit,
                    IsAllergen = viewModel.IsAllergen
                };

                var createdIngredient = await _ingredientService.CreateIngredientAsync(createDto);

                TempData["SuccessMessage"] = $"Ingredient '{createdIngredient.IngredientName}' created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while creating ingredient");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(viewModel);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Business error while creating ingredient");
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating ingredient");
                ModelState.AddModelError(string.Empty, "An error occurred while creating the ingredient.");
                return View(viewModel);
            }
        }

        // GET: Ingredient/Edit/{id} - Show edit ingredient form
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var ingredientDto = await _ingredientService.GetByIdAsync(id);
                
                if (ingredientDto == null)
                {
                    return NotFound("Ingredient not found.");
                }

                // Get all allergies for selection
                var allergyService = HttpContext.RequestServices.GetRequiredService<IAllergyService>();
                var allAllergies = await allergyService.GetAllAsync();

                var viewModel = new EditIngredientViewModel
                {
                    Id = ingredientDto.Id,
                    IngredientName = ingredientDto.IngredientName,
                    Unit = ingredientDto.Unit,
                    CaloPerUnit = ingredientDto.CaloPerUnit,
                    IsAllergen = ingredientDto.IsAllergen,
                    SelectedAllergyIds = ingredientDto.Allergies.Select(a => a.Id).ToList(),
                    AvailableAllergies = allAllergies.Select(a => new AllergyViewModel
                    {
                        Id = a.Id,
                        AllergyName = a.AllergyName,
                        IsSelected = ingredientDto.Allergies.Any(ia => ia.Id == a.Id)
                    }).ToList()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading ingredient {IngredientId} for editing", id);
                TempData["ErrorMessage"] = "An error occurred while loading the ingredient.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Ingredient/Edit/{id} - Update an existing ingredient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditIngredientViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return BadRequest("Ingredient ID mismatch.");
            }

            if (!ModelState.IsValid)
            {
                // Reload allergies for the form
                var allergyService = HttpContext.RequestServices.GetRequiredService<IAllergyService>();
                var allAllergies = await allergyService.GetAllAsync();
                viewModel.AvailableAllergies = allAllergies.Select(a => new AllergyViewModel
                {
                    Id = a.Id,
                    AllergyName = a.AllergyName,
                    IsSelected = viewModel.SelectedAllergyIds.Contains(a.Id)
                }).ToList();
                return View(viewModel);
            }

            try
            {
                var updateDto = new UpdateIngredientDto
                {
                    IngredientName = viewModel.IngredientName,
                    Unit = viewModel.Unit,
                    CaloPerUnit = viewModel.CaloPerUnit,
                    IsAllergen = viewModel.IsAllergen,
                    AllergyIds = viewModel.SelectedAllergyIds ?? new List<Guid>()
                };

                var updatedIngredient = await _ingredientService.UpdateIngredientAsync(id, updateDto);

                TempData["SuccessMessage"] = $"Ingredient '{updatedIngredient.IngredientName}' updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Ingredient {IngredientId} not found", id);
                return NotFound(ex.Message);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error while updating ingredient {IngredientId}", id);
                ModelState.AddModelError(string.Empty, ex.Message);
                
                // Reload allergies for the form
                var allergyService = HttpContext.RequestServices.GetRequiredService<IAllergyService>();
                var allAllergies = await allergyService.GetAllAsync();
                viewModel.AvailableAllergies = allAllergies.Select(a => new AllergyViewModel
                {
                    Id = a.Id,
                    AllergyName = a.AllergyName,
                    IsSelected = viewModel.SelectedAllergyIds.Contains(a.Id)
                }).ToList();
                return View(viewModel);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Business error while updating ingredient {IngredientId}", id);
                ModelState.AddModelError(string.Empty, ex.Message);
                
                // Reload allergies for the form
                var allergyService = HttpContext.RequestServices.GetRequiredService<IAllergyService>();
                var allAllergies = await allergyService.GetAllAsync();
                viewModel.AvailableAllergies = allAllergies.Select(a => new AllergyViewModel
                {
                    Id = a.Id,
                    AllergyName = a.AllergyName,
                    IsSelected = viewModel.SelectedAllergyIds.Contains(a.Id)
                }).ToList();
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating ingredient {IngredientId}", id);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the ingredient.");
                
                // Reload allergies for the form
                var allergyService = HttpContext.RequestServices.GetRequiredService<IAllergyService>();
                var allAllergies = await allergyService.GetAllAsync();
                viewModel.AvailableAllergies = allAllergies.Select(a => new AllergyViewModel
                {
                    Id = a.Id,
                    AllergyName = a.AllergyName,
                    IsSelected = viewModel.SelectedAllergyIds.Contains(a.Id)
                }).ToList();
                return View(viewModel);
            }
        }

        // POST: Ingredient/Delete/{id} - Delete an ingredient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _ingredientService.DeleteIngredientAsync(id);

                TempData["SuccessMessage"] = "Ingredient deleted successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Ingredient {IngredientId} not found", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (ConstraintViolationException ex)
            {
                _logger.LogWarning(ex, "Cannot delete ingredient {IngredientId} due to constraint violation", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Business error while deleting ingredient {IngredientId}", id);
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting ingredient {IngredientId}", id);
                TempData["ErrorMessage"] = "An error occurred while deleting the ingredient.";
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        #region Helper Methods

        /// <summary>
        /// Maps IngredientDto to IngredientViewModel
        /// </summary>
        private IngredientViewModel MapToViewModel(IngredientDto dto)
        {
            return new IngredientViewModel
            {
                Id = dto.Id,
                IngredientName = dto.IngredientName,
                Unit = dto.Unit,
                CaloPerUnit = dto.CaloPerUnit,
                IsAllergen = dto.IsAllergen,
                Allergies = dto.Allergies?.Select(a => new AllergyViewModel
                {
                    Id = a.Id,
                    AllergyName = a.AllergyName
                }).ToList() ?? new List<AllergyViewModel>()
            };
        }

        #endregion
    }
}
