using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MealPrepService.BusinessLogicLayer.Interfaces;
using MealPrepService.BusinessLogicLayer.DTOs;
using MealPrepService.Web.PresentationLayer.ViewModels;

namespace MealPrepService.Web.PresentationLayer.Controllers
{
    [AllowAnonymous] // Allow both guests and authenticated users
    public class PublicMenuController : Controller
    {
        private readonly IMenuService _menuService;
        private readonly ILogger<PublicMenuController> _logger;

        public PublicMenuController(
            IMenuService menuService,
            ILogger<PublicMenuController> logger)
        {
            _menuService = menuService;
            _logger = logger;
        }

        // GET: PublicMenu/Today - View today's menu
        [HttpGet]
        public async Task<IActionResult> Today()
        {
            try
            {
                var today = DateTime.Today;
                var menuDto = await _menuService.GetByDateAsync(today);
                
                if (menuDto == null || !menuDto.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
                {
                    var viewModel = new PublicMenuViewModel
                    {
                        MenuDate = today,
                        AvailableMeals = new List<PublicMenuMealViewModel>()
                    };
                    
                    ViewBag.NoMenuMessage = "No menu is available for today. Please check back later.";
                    return View(viewModel);
                }

                var publicMenuViewModel = MapToPublicViewModel(menuDto);
                
                return View(publicMenuViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving today's menu");
                
                var errorViewModel = new PublicMenuViewModel
                {
                    MenuDate = DateTime.Today,
                    AvailableMeals = new List<PublicMenuMealViewModel>()
                };
                
                ViewBag.ErrorMessage = "An error occurred while loading today's menu. Please try again later.";
                return View(errorViewModel);
            }
        }

        // GET: PublicMenu/Weekly - View weekly menu
        [HttpGet]
        public async Task<IActionResult> Weekly(DateTime? startDate = null)
        {
            try
            {
                // Default to current week if no start date provided
                var weekStart = startDate ?? GetStartOfWeek(DateTime.Today);
                var weekEnd = weekStart.AddDays(6);
                
                var weeklyMenus = await _menuService.GetWeeklyMenuAsync(weekStart);
                
                var dailyMenuViewModels = new List<PublicMenuViewModel>();
                
                // Create view models for each day of the week
                for (var date = weekStart; date <= weekEnd; date = date.AddDays(1))
                {
                    var menuForDate = weeklyMenus.FirstOrDefault(m => m.MenuDate.Date == date.Date);
                    
                    if (menuForDate != null && menuForDate.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
                    {
                        dailyMenuViewModels.Add(MapToPublicViewModel(menuForDate));
                    }
                    else
                    {
                        // Add empty menu for days without active menus
                        dailyMenuViewModels.Add(new PublicMenuViewModel
                        {
                            MenuDate = date,
                            AvailableMeals = new List<PublicMenuMealViewModel>()
                        });
                    }
                }

                var weeklyViewModel = new WeeklyMenuViewModel
                {
                    WeekStartDate = weekStart,
                    WeekEndDate = weekEnd,
                    DailyMenus = dailyMenuViewModels
                };
                
                return View(weeklyViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving weekly menu for week starting {StartDate}", startDate);
                
                var weekStart = startDate ?? GetStartOfWeek(DateTime.Today);
                var errorViewModel = new WeeklyMenuViewModel
                {
                    WeekStartDate = weekStart,
                    WeekEndDate = weekStart.AddDays(6),
                    DailyMenus = new List<PublicMenuViewModel>()
                };
                
                ViewBag.ErrorMessage = "An error occurred while loading the weekly menu. Please try again later.";
                return View(errorViewModel);
            }
        }

        // GET: PublicMenu/Date/{date} - View menu for specific date
        [HttpGet]
        public async Task<IActionResult> Date(DateTime date)
        {
            try
            {
                var menuDto = await _menuService.GetByDateAsync(date.Date);
                
                if (menuDto == null || !menuDto.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
                {
                    var viewModel = new PublicMenuViewModel
                    {
                        MenuDate = date.Date,
                        AvailableMeals = new List<PublicMenuMealViewModel>()
                    };
                    
                    ViewBag.NoMenuMessage = $"No menu is available for {date:dddd, MMMM dd, yyyy}. Please check another date.";
                    return View("Today", viewModel);
                }

                var publicMenuViewModel = MapToPublicViewModel(menuDto);
                
                return View("Today", publicMenuViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving menu for date {Date}", date);
                
                var errorViewModel = new PublicMenuViewModel
                {
                    MenuDate = date.Date,
                    AvailableMeals = new List<PublicMenuMealViewModel>()
                };
                
                ViewBag.ErrorMessage = $"An error occurred while loading the menu for {date:dddd, MMMM dd, yyyy}. Please try again later.";
                return View("Today", errorViewModel);
            }
        }

        // GET: PublicMenu/NextWeek - Navigate to next week
        [HttpGet]
        public IActionResult NextWeek(DateTime currentWeekStart)
        {
            var nextWeekStart = currentWeekStart.AddDays(7);
            return RedirectToAction(nameof(Weekly), new { startDate = nextWeekStart });
        }

        // GET: PublicMenu/PreviousWeek - Navigate to previous week
        [HttpGet]
        public IActionResult PreviousWeek(DateTime currentWeekStart)
        {
            var previousWeekStart = currentWeekStart.AddDays(-7);
            return RedirectToAction(nameof(Weekly), new { startDate = previousWeekStart });
        }

        #region Private Helper Methods

        private PublicMenuViewModel MapToPublicViewModel(DailyMenuDto dto)
        {
            return new PublicMenuViewModel
            {
                MenuDate = dto.MenuDate,
                AvailableMeals = dto.MenuMeals
                    .Where(m => !m.IsSoldOut) // Only show available meals to public
                    .Select(MapToPublicMenuMealViewModel)
                    .ToList()
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

        private DateTime GetStartOfWeek(DateTime date)
        {
            // Get the start of the week (Sunday)
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        #endregion
    }
}