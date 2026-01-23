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
    [Authorize(Roles = "Admin,Manager")]
    public class MenuController : Controller
    {
        private readonly IMenuService _menuService;
        private readonly IRecipeService _recipeService;
        private readonly ILogger<MenuController> _logger;

        public MenuController(
            IMenuService menuService,
            IRecipeService recipeService,
            ILogger<MenuController> logger)
        {
            _menuService = menuService;
            _recipeService = recipeService;
            _logger = logger;
        }

        // GET: Menu/Index - List all menus
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // Get menus for the next 30 days
                var startDate = DateTime.Today;
                var endDate = DateTime.Today.AddDays(30);
                
                var menuList = new List<DailyMenuDto>();
                
                // Get menus for each day in the range
                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var menu = await _menuService.GetByDateAsync(date);
                    if (menu != null)
                    {
                        menuList.Add(menu);
                    }
                }

                var viewModels = menuList.Select(MapToViewModel).OrderBy(m => m.MenuDate).ToList();
                
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                
                return View(viewModels);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving menus");
                TempData["ErrorMessage"] = "An error occurred while loading the menus.";
                return View(new List<DailyMenuViewModel>());
            }
        }

        // GET: Menu/Details/{id} - View menu details
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                // Find menu by ID (we need to search through dates since we don't have a direct GetById method)
                DailyMenuDto? menuDto = null;
                
                // Search through recent dates to find the menu
                for (var date = DateTime.Today.AddDays(-30); date <= DateTime.Today.AddDays(30); date = date.AddDays(1))
                {
                    var menu = await _menuService.GetByDateAsync(date);
                    if (menu?.Id == id)
                    {
                        menuDto = menu;
                        break;
                    }
                }
                
                if (menuDto == null)
                {
                    return NotFound("Menu not found.");
                }

                var viewModel = MapToViewModel(menuDto);
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving menu {MenuId}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the menu.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Menu/Create - Show create menu form
        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new CreateMenuViewModel
            {
                MenuDate = DateTime.Today
            };
            
            return View(viewModel);
        }

        // POST: Menu/Create - Create daily menu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMenuViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Check if menu already exists for this date
                var existingMenu = await _menuService.GetByDateAsync(model.MenuDate);
                if (existingMenu != null)
                {
                    ModelState.AddModelError(nameof(model.MenuDate), "A menu already exists for this date.");
                    return View(model);
                }

                var createdMenu = await _menuService.CreateDailyMenuAsync(model.MenuDate);
                
                _logger.LogInformation("Daily menu created successfully for date {MenuDate}", model.MenuDate);
                
                TempData["SuccessMessage"] = "Menu created successfully!";
                return RedirectToAction(nameof(Details), new { id = createdMenu.Id });
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating menu for date {MenuDate}", model.MenuDate);
                ModelState.AddModelError(string.Empty, "An error occurred while creating the menu. Please try again.");
                return View(model);
            }
        }

        // GET: Menu/AddMeal/{menuId} - Show add meal form
        [HttpGet]
        public async Task<IActionResult> AddMeal(Guid menuId)
        {
            try
            {
                // Find menu by ID
                DailyMenuDto? menuDto = null;
                
                // Search through recent dates to find the menu
                for (var date = DateTime.Today.AddDays(-30); date <= DateTime.Today.AddDays(30); date = date.AddDays(1))
                {
                    var menu = await _menuService.GetByDateAsync(date);
                    if (menu?.Id == menuId)
                    {
                        menuDto = menu;
                        break;
                    }
                }
                
                if (menuDto == null)
                {
                    return NotFound("Menu not found.");
                }

                // Check if menu is still in draft status
                if (!menuDto.Status.Equals("draft", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Cannot add meals to a published menu.";
                    return RedirectToAction(nameof(Details), new { id = menuId });
                }

                var recipes = await _recipeService.GetAllAsync();
                
                var viewModel = new AddMealToMenuViewModel
                {
                    MenuId = menuId,
                    AvailableRecipes = recipes.Select(r => new MenuRecipeSelectionViewModel
                    {
                        Id = r.Id,
                        RecipeName = r.RecipeName,
                        TotalCalories = r.TotalCalories,
                        ProteinG = r.ProteinG,
                        FatG = r.FatG,
                        CarbsG = r.CarbsG,
                        IsSelected = false
                    }).ToList(),
                    MenuDateDisplay = menuDto.MenuDate.ToString("dddd, MMMM dd, yyyy")
                };
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading add meal form for menu {MenuId}", menuId);
                TempData["ErrorMessage"] = "An error occurred while loading the form.";
                return RedirectToAction(nameof(Details), new { id = menuId });
            }
        }

        // POST: Menu/AddMeal - Add meal to menu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMeal(AddMealToMenuViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Reload recipes for the form
                var recipes = await _recipeService.GetAllAsync();
                model.AvailableRecipes = recipes.Select(r => new MenuRecipeSelectionViewModel
                {
                    Id = r.Id,
                    RecipeName = r.RecipeName,
                    TotalCalories = r.TotalCalories,
                    ProteinG = r.ProteinG,
                    FatG = r.FatG,
                    CarbsG = r.CarbsG,
                    IsSelected = r.Id == model.RecipeId
                }).ToList();
                
                return View(model);
            }

            try
            {
                var recipe = await _recipeService.GetByIdAsync(model.RecipeId);
                if (recipe == null)
                {
                    ModelState.AddModelError(nameof(model.RecipeId), "Selected recipe not found.");
                    
                    // Reload recipes for the form
                    var recipes = await _recipeService.GetAllAsync();
                    model.AvailableRecipes = recipes.Select(r => new MenuRecipeSelectionViewModel
                    {
                        Id = r.Id,
                        RecipeName = r.RecipeName,
                        TotalCalories = r.TotalCalories,
                        ProteinG = r.ProteinG,
                        FatG = r.FatG,
                        CarbsG = r.CarbsG,
                        IsSelected = r.Id == model.RecipeId
                    }).ToList();
                    
                    return View(model);
                }

                var menuMealDto = new MenuMealDto
                {
                    MenuId = model.MenuId,
                    RecipeId = model.RecipeId,
                    RecipeName = recipe.RecipeName,
                    Price = model.Price,
                    AvailableQuantity = model.AvailableQuantity,
                    IsSoldOut = false,
                    Recipe = recipe
                };

                await _menuService.AddMealToMenuAsync(model.MenuId, menuMealDto);
                
                _logger.LogInformation("Meal {RecipeName} added successfully to menu {MenuId}", recipe.RecipeName, model.MenuId);
                
                TempData["SuccessMessage"] = "Meal added to menu successfully!";
                return RedirectToAction(nameof(Details), new { id = model.MenuId });
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                
                // Reload recipes for the form
                var recipes = await _recipeService.GetAllAsync();
                model.AvailableRecipes = recipes.Select(r => new MenuRecipeSelectionViewModel
                {
                    Id = r.Id,
                    RecipeName = r.RecipeName,
                    TotalCalories = r.TotalCalories,
                    ProteinG = r.ProteinG,
                    FatG = r.FatG,
                    CarbsG = r.CarbsG,
                    IsSelected = r.Id == model.RecipeId
                }).ToList();
                
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding meal to menu {MenuId}", model.MenuId);
                ModelState.AddModelError(string.Empty, "An error occurred while adding the meal. Please try again.");
                
                // Reload recipes for the form
                var recipes = await _recipeService.GetAllAsync();
                model.AvailableRecipes = recipes.Select(r => new MenuRecipeSelectionViewModel
                {
                    Id = r.Id,
                    RecipeName = r.RecipeName,
                    TotalCalories = r.TotalCalories,
                    ProteinG = r.ProteinG,
                    FatG = r.FatG,
                    CarbsG = r.CarbsG,
                    IsSelected = r.Id == model.RecipeId
                }).ToList();
                
                return View(model);
            }
        }

        // POST: Menu/Publish/{menuId} - Publish menu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Publish(Guid menuId)
        {
            try
            {
                await _menuService.PublishMenuAsync(menuId);
                
                _logger.LogInformation("Menu {MenuId} published successfully", menuId);
                
                TempData["SuccessMessage"] = "Menu published successfully!";
                return RedirectToAction(nameof(Details), new { id = menuId });
            }
            catch (BusinessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = menuId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while publishing menu {MenuId}", menuId);
                TempData["ErrorMessage"] = "An error occurred while publishing the menu. Please try again.";
                return RedirectToAction(nameof(Details), new { id = menuId });
            }
        }

        // GET: Menu/UpdateQuantity/{menuMealId} - Show update quantity form
        [HttpGet]
        public async Task<IActionResult> UpdateQuantity(Guid menuMealId)
        {
            try
            {
                // Find the menu meal by searching through menus
                MenuMealDto? menuMealDto = null;
                DailyMenuDto? parentMenu = null;
                
                // Search through recent dates to find the menu meal
                for (var date = DateTime.Today.AddDays(-30); date <= DateTime.Today.AddDays(30); date = date.AddDays(1))
                {
                    var menu = await _menuService.GetByDateAsync(date);
                    if (menu != null)
                    {
                        var meal = menu.MenuMeals.FirstOrDefault(m => m.Id == menuMealId);
                        if (meal != null)
                        {
                            menuMealDto = meal;
                            parentMenu = menu;
                            break;
                        }
                    }
                }
                
                if (menuMealDto == null || parentMenu == null)
                {
                    return NotFound("Menu meal not found.");
                }

                // Check if menu is still in draft status
                if (!parentMenu.Status.Equals("draft", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Cannot update quantities for a published menu.";
                    return RedirectToAction(nameof(Details), new { id = parentMenu.Id });
                }

                var viewModel = new UpdateMenuQuantityViewModel
                {
                    MenuMealId = menuMealId,
                    NewQuantity = menuMealDto.AvailableQuantity,
                    RecipeName = menuMealDto.RecipeName,
                    CurrentQuantity = menuMealDto.AvailableQuantity
                };
                
                ViewBag.MenuId = parentMenu.Id;
                ViewBag.MenuDate = parentMenu.MenuDate.ToString("dddd, MMMM dd, yyyy");
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading update quantity form for menu meal {MenuMealId}", menuMealId);
                TempData["ErrorMessage"] = "An error occurred while loading the form.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Menu/UpdateQuantity - Update meal quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(UpdateMenuQuantityViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                await _menuService.UpdateMealQuantityAsync(model.MenuMealId, model.NewQuantity);
                
                _logger.LogInformation("Menu meal {MenuMealId} quantity updated to {NewQuantity}", 
                    model.MenuMealId, model.NewQuantity);
                
                TempData["SuccessMessage"] = "Quantity updated successfully!";
                
                // Find the parent menu to redirect back to details
                for (var date = DateTime.Today.AddDays(-30); date <= DateTime.Today.AddDays(30); date = date.AddDays(1))
                {
                    var menu = await _menuService.GetByDateAsync(date);
                    if (menu != null && menu.MenuMeals.Any(m => m.Id == model.MenuMealId))
                    {
                        return RedirectToAction(nameof(Details), new { id = menu.Id });
                    }
                }
                
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating quantity for menu meal {MenuMealId}", model.MenuMealId);
                ModelState.AddModelError(string.Empty, "An error occurred while updating the quantity. Please try again.");
                return View(model);
            }
        }

        #region Private Helper Methods

        private DailyMenuViewModel MapToViewModel(DailyMenuDto dto)
        {
            return new DailyMenuViewModel
            {
                Id = dto.Id,
                MenuDate = dto.MenuDate,
                Status = dto.Status,
                MenuMeals = dto.MenuMeals.Select(MapMenuMealToViewModel).ToList()
            };
        }

        private MenuMealViewModel MapMenuMealToViewModel(MenuMealDto dto)
        {
            return new MenuMealViewModel
            {
                Id = dto.Id,
                MenuId = dto.MenuId,
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

        #endregion
    }
}