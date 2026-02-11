using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.BusinessLogicLayer.Exceptions;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.Web.PresentationLayer.ViewModels;

namespace MealPrepService.Web.PresentationLayer.Controllers;

[Authorize(Roles = "Manager,Admin")]
public class AllergyController : Controller
{
    private readonly IAllergyService _allergyService;
    private readonly ILogger<AllergyController> _logger;

    public AllergyController(IAllergyService allergyService, ILogger<AllergyController> logger)
    {
        _allergyService = allergyService;
        _logger = logger;
    }

    // GET: Allergy/Index
    [HttpGet]
    public async Task<IActionResult> Index(string searchTerm = "", bool showAll = false, int page = 1)
    {
        try
        {
            const int pageSize = 30;
            var viewModels = new List<AllergyViewModel>();

            // Load allergies if search term is provided OR showAll is true
            if (!string.IsNullOrWhiteSpace(searchTerm) || showAll)
            {
                var allergies = await _allergyService.GetAllAsync();
                
                // Apply search filter only if search term is provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    viewModels = allergies
                        .Where(a => a.AllergyName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        .Select(a => new AllergyViewModel
                        {
                            Id = a.Id,
                            AllergyName = a.AllergyName
                        }).ToList();
                }
                else
                {
                    viewModels = allergies.Select(a => new AllergyViewModel
                    {
                        Id = a.Id,
                        AllergyName = a.AllergyName
                    }).ToList();
                }
            }

            // Calculate pagination
            var totalItems = viewModels.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));
            
            var pagedAllergies = viewModels
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var listViewModel = new AllergyListViewModel
            {
                Allergies = pagedAllergies,
                SearchTerm = searchTerm,
                ShowAll = showAll,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            return View(listViewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving allergies");
            TempData["ErrorMessage"] = "An error occurred while loading allergies.";
            return View(new AllergyListViewModel());
        }
    }

    // GET: Allergy/Create
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateAllergyViewModel());
    }

    // POST: Allergy/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAllergyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var createDto = new CreateAllergyDto
            {
                AllergyName = model.AllergyName
            };

            await _allergyService.CreateAsync(createDto);

            _logger.LogInformation("Allergy {AllergyName} created successfully", model.AllergyName);
            TempData["SuccessMessage"] = $"Allergy '{model.AllergyName}' created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating allergy");
            ModelState.AddModelError(string.Empty, "An error occurred while creating the allergy. Please try again.");
            return View(model);
        }
    }

    // GET: Allergy/Edit/{id}
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var allergy = await _allergyService.GetByIdAsync(id);
            if (allergy == null)
            {
                TempData["ErrorMessage"] = "Allergy not found.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new EditAllergyViewModel
            {
                Id = allergy.Id,
                AllergyName = allergy.AllergyName
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading allergy {AllergyId} for editing", id);
            TempData["ErrorMessage"] = "An error occurred while loading the allergy.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Allergy/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditAllergyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var updateDto = new UpdateAllergyDto
            {
                Id = model.Id,
                AllergyName = model.AllergyName
            };

            await _allergyService.UpdateAsync(updateDto);

            _logger.LogInformation("Allergy {AllergyId} updated successfully", model.Id);
            TempData["SuccessMessage"] = $"Allergy '{model.AllergyName}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (ValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating allergy {AllergyId}", model.Id);
            ModelState.AddModelError(string.Empty, "An error occurred while updating the allergy. Please try again.");
            return View(model);
        }
    }

    // GET: Allergy/Delete/{id}
    [HttpGet]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var allergy = await _allergyService.GetByIdAsync(id);
            if (allergy == null)
            {
                TempData["ErrorMessage"] = "Allergy not found.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new AllergyViewModel
            {
                Id = allergy.Id,
                AllergyName = allergy.AllergyName
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading allergy {AllergyId} for deletion", id);
            TempData["ErrorMessage"] = "An error occurred while loading the allergy.";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Allergy/DeleteConfirmed
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        try
        {
            await _allergyService.DeleteAsync(id);

            _logger.LogInformation("Allergy {AllergyId} deleted successfully", id);
            TempData["SuccessMessage"] = "Allergy deleted successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (NotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (ConstraintViolationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting allergy {AllergyId}", id);
            TempData["ErrorMessage"] = "An error occurred while deleting the allergy. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }
}
