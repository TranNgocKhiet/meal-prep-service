using MealPreparationService.API.Models;
using MealPreparationService.DataAccess.UnitOfWork;
using MealPreparationService.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MealPreparationService.API.Controllers;

public class CreateDailyMenuDto
{
    public DateTime MenuDate { get; set; }
    // StatusId will be set to MenuDrafted (16) automatically on creation
}

public class UpdateMenuStatusDto
{
    public int StatusId { get; set; } // 16=MenuDrafted, 17=MenuActive, 18=MenuInactive
}

public class UpdateMealDetailsDto
{
    public decimal Price { get; set; }
    public int AvailibleQuantity { get; set; }
}

public class AddRecipesToMealDto
{
    public List<string> RecipeIds { get; set; } = new();
}

public class DailyMenuDto
{
    public string? Id { get; set; }
    public int StatusId { get; set; }
    public DateTime MenuDate { get; set; }
    public List<MenuMealDto> MenuMeals { get; set; } = new();
}

public class MenuMealDto
{
    public string? Id { get; set; }
    public int MealTypeId { get; set; }
    public decimal Price { get; set; }
    public int AvailibleQuantity { get; set; }
    public List<string> RecipeIds { get; set; } = new();
}

[ApiController]
[Route("api/dailymenus")]
public class DailyMenuController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DailyMenuController> _logger;

    public DailyMenuController(IUnitOfWork unitOfWork, ILogger<DailyMenuController> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<DailyMenu>>>> GetDailyMenus(
        [FromQuery] DateTime? date = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool includeAll = false)
    {
        try
        {
            var query = _unitOfWork.DailyMenus.GetAllQueryable();

            // Only show active menus (StatusId = 17) for public access unless includeAll is true
            if (!includeAll)
            {
                query = query.Where(m => m.StatusId == 17);
            }

            if (date.HasValue)
            {
                var dateOnly = date.Value.Date;
                query = query.Where(m => m.MenuDate.Date == dateOnly);
            }
            else if (startDate.HasValue && endDate.HasValue)
            {
                var start = startDate.Value.Date;
                var end = endDate.Value.Date;
                query = query.Where(m => m.MenuDate.Date >= start && m.MenuDate.Date <= end);
            }

            var menus = await query
                .Include(m => m.MenuMeals)
                    .ThenInclude(mm => mm.MenuMealRecipes)
                        .ThenInclude(mmr => mmr.Recipe)
                .OrderBy(m => m.MenuDate)
                .AsNoTracking()
                .ToListAsync();

            return Ok(new ApiResponse<IEnumerable<DailyMenu>>
            {
                Success = true,
                Data = menus,
                Message = "Daily menus retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving daily menus");
            return StatusCode(500, new ApiResponse<IEnumerable<DailyMenu>>
            {
                Success = false,
                Message = "An error occurred while retrieving daily menus"
            });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<DailyMenu>>> GetDailyMenu(string id)
    {
        try
        {
            var menu = await _unitOfWork.DailyMenus.GetAllQueryable()
                .Include(m => m.MenuMeals)
                    .ThenInclude(mm => mm.MenuMealRecipes)
                        .ThenInclude(mmr => mmr.Recipe)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menu == null)
            {
                return NotFound(new ApiResponse<DailyMenu>
                {
                    Success = false,
                    Message = "Daily menu not found"
                });
            }

            return Ok(new ApiResponse<DailyMenu>
            {
                Success = true,
                Data = menu,
                Message = "Daily menu retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving daily menu {Id}", id);
            return StatusCode(500, new ApiResponse<DailyMenu>
            {
                Success = false,
                Message = "An error occurred while retrieving the daily menu"
            });
        }
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DailyMenu>>> CreateDailyMenu([FromBody] CreateDailyMenuDto dto)
    {
        try
        {
            var menu = new DailyMenu
            {
                Id = Guid.NewGuid().ToString(),
                StatusId = 16, // MenuDrafted - always start as draft
                MenuDate = dto.MenuDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.DailyMenus.AddAsync(menu);

            // Automatically create three meals: Breakfast (1), Lunch (2), Dinner (3)
            var mealTypes = new[] { 1, 2, 3 }; // Breakfast, Lunch, Dinner
            foreach (var mealTypeId in mealTypes)
            {
                var menuMeal = new MenuMeal
                {
                    Id = Guid.NewGuid().ToString(),
                    MenuId = menu.Id,
                    MealTypeId = mealTypeId,
                    Price = 0, // Default price, admin will set later
                    AvailableQuantity = 0, // Default quantity, admin will set later
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.MenuMeals.AddAsync(menuMeal);
            }

            // Reload with includes
            var created = await _unitOfWork.DailyMenus.GetAllQueryable()
                .Include(m => m.MenuMeals)
                    .ThenInclude(mm => mm.MenuMealRecipes)
                        .ThenInclude(mmr => mmr.Recipe)
                .FirstOrDefaultAsync(m => m.Id == menu.Id);

            return CreatedAtAction(nameof(GetDailyMenu), new { id = menu.Id }, new ApiResponse<DailyMenu>
            {
                Success = true,
                Data = created,
                Message = "Daily menu created successfully with three meals (Breakfast, Lunch, Dinner)"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating daily menu");
            return StatusCode(500, new ApiResponse<DailyMenu>
            {
                Success = false,
                Message = "An error occurred while creating the daily menu"
            });
        }
    }

    [Authorize]
    [HttpPost("{menuId}/meals/{mealId}/recipes")]
    public async Task<ActionResult<ApiResponse<MenuMeal>>> AddRecipesToMeal(string menuId, string mealId, [FromBody] AddRecipesToMealDto dto)
    {
        try
        {
            // Get the menu to check its status
            var menu = await _unitOfWork.DailyMenus.GetByIdAsync(menuId);
            if (menu == null)
            {
                return NotFound(new ApiResponse<MenuMeal>
                {
                    Success = false,
                    Message = "Daily menu not found"
                });
            }

            // Check if menu is active (StatusId = 17)
            if (menu.StatusId == 17)
            {
                return BadRequest(new ApiResponse<MenuMeal>
                {
                    Success = false,
                    Message = "Cannot edit recipes while menu is active. Please deactivate the menu first."
                });
            }

            var menuMeal = await _unitOfWork.MenuMeals.GetAllQueryable()
                .Include(mm => mm.MenuMealRecipes)
                .FirstOrDefaultAsync(mm => mm.Id == mealId && mm.MenuId == menuId);

            if (menuMeal == null)
            {
                return NotFound(new ApiResponse<MenuMeal>
                {
                    Success = false,
                    Message = "Menu meal not found"
                });
            }

            // Remove existing recipes (convert to list first to avoid collection modification)
            var existingRecipes = await _unitOfWork.MenuMealRecipes.GetByMenuMealIdAsync(mealId);
            foreach (var existing in existingRecipes)
            {
                await _unitOfWork.MenuMealRecipes.DeleteAsync(existing.MenuMealId, existing.RecipeId);
            }

            // Add new recipes
            foreach (var recipeId in dto.RecipeIds)
            {
                var menuMealRecipe = new MenuMealRecipe
                {
                    MenuMealId = mealId,
                    RecipeId = recipeId
                };
                await _unitOfWork.MenuMealRecipes.AddAsync(menuMealRecipe);
            }

            // Calculate nutritional values from recipes
            var nutritionTotals = await CalculateMealNutrition(dto.RecipeIds);
            
            // Update MenuMeal with calculated nutrition
            menuMeal.TotalCalories = nutritionTotals.Calories;
            menuMeal.ProteinG = nutritionTotals.Protein;
            menuMeal.FatG = nutritionTotals.Fat;
            menuMeal.CarbsG = nutritionTotals.Carbs;
            menuMeal.UpdatedAt = DateTime.UtcNow;
            
            await _unitOfWork.MenuMeals.UpdateAsync(menuMeal);

            // Reload with minimal includes to avoid circular reference
            var updated = await _unitOfWork.MenuMeals.GetAllQueryable()
                .Include(mm => mm.MenuMealRecipes)
                    .ThenInclude(mmr => mmr.Recipe)
                .AsNoTracking()
                .FirstOrDefaultAsync(mm => mm.Id == mealId);

            var message = dto.RecipeIds.Count == 0 
                ? "All recipes removed from meal" 
                : $"Recipes updated successfully ({dto.RecipeIds.Count} recipe(s))";

            return Ok(new ApiResponse<MenuMeal>
            {
                Success = true,
                Data = updated,
                Message = message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding recipes to meal {MealId}", mealId);
            return StatusCode(500, new ApiResponse<MenuMeal>
            {
                Success = false,
                Message = "An error occurred while adding recipes"
            });
        }
    }

    private async Task<(decimal Calories, decimal Protein, decimal Fat, decimal Carbs)> CalculateMealNutrition(List<string> recipeIds)
    {
        decimal totalCalories = 0;
        decimal totalProtein = 0;
        decimal totalFat = 0;
        decimal totalCarbs = 0;

        foreach (var recipeId in recipeIds)
        {
            // Get recipe with ingredients and nutrients - use AsNoTracking to avoid tracking issues
            var recipe = await _unitOfWork.Recipes.GetAllQueryable()
                .AsNoTracking()
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                        .ThenInclude(i => i.IngredientNutrients)
                            .ThenInclude(inu => inu.Nutrient)
                .AsSplitQuery() // Use split query to avoid cartesian explosion
                .FirstOrDefaultAsync(r => r.Id == recipeId);

            if (recipe != null)
            {
                foreach (var recipeIngredient in recipe.RecipeIngredients)
                {
                    var amount = recipeIngredient.Amount; // Amount in grams or ml
                    
                    foreach (var ingredientNutrient in recipeIngredient.Ingredient.IngredientNutrients)
                    {
                        var nutrientName = ingredientNutrient.Nutrient.Name.ToLower().Trim();
                        var amountPer100 = ingredientNutrient.AmountPer100;
                        var actualAmount = (amount / 100) * amountPer100;

                        // Map nutrients to categories - be more flexible with matching
                        if (nutrientName.Contains("energy") || nutrientName.Contains("calor") || nutrientName.Contains("kcal"))
                        {
                            totalCalories += actualAmount;
                        }
                        else if (nutrientName.Contains("protein") || nutrientName == "protein")
                        {
                            totalProtein += actualAmount;
                        }
                        else if ((nutrientName.Contains("fat") || nutrientName.Contains("lipid")) && 
                                 !nutrientName.Contains("saturated") && 
                                 !nutrientName.Contains("trans") &&
                                 !nutrientName.Contains("mono") &&
                                 !nutrientName.Contains("poly"))
                        {
                            totalFat += actualAmount;
                        }
                        else if (nutrientName.Contains("carb") || nutrientName.Contains("carbohydrate"))
                        {
                            totalCarbs += actualAmount;
                        }
                    }
                }
            }
        }

        _logger.LogInformation("Calculated nutrition for recipes {RecipeIds}: Calories={Calories}, Protein={Protein}, Fat={Fat}, Carbs={Carbs}", 
            string.Join(",", recipeIds), totalCalories, totalProtein, totalFat, totalCarbs);

        return (totalCalories, totalProtein, totalFat, totalCarbs);
    }

    [Authorize]
    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<DailyMenu>>> UpdateMenuStatus(string id, [FromBody] UpdateMenuStatusDto dto)
    {
        try
        {
            var menu = await _unitOfWork.DailyMenus.GetByIdAsync(id);
            if (menu == null)
            {
                return NotFound(new ApiResponse<DailyMenu>
                {
                    Success = false,
                    Message = "Daily menu not found"
                });
            }

            // Validate status ID (16=MenuDrafted, 17=MenuActive, 18=MenuInactive)
            if (dto.StatusId < 16 || dto.StatusId > 18)
            {
                return BadRequest(new ApiResponse<DailyMenu>
                {
                    Success = false,
                    Message = "Invalid status ID. Must be 16 (Draft), 17 (Active), or 18 (Inactive)"
                });
            }

            menu.StatusId = dto.StatusId;
            menu.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.DailyMenus.UpdateAsync(menu);

            // Reload with includes
            var updated = await _unitOfWork.DailyMenus.GetAllQueryable()
                .Include(m => m.MenuMeals)
                    .ThenInclude(mm => mm.MenuMealRecipes)
                        .ThenInclude(mmr => mmr.Recipe)
                .FirstOrDefaultAsync(m => m.Id == id);

            return Ok(new ApiResponse<DailyMenu>
            {
                Success = true,
                Data = updated,
                Message = "Menu status updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating menu status {Id}", id);
            return StatusCode(500, new ApiResponse<DailyMenu>
            {
                Success = false,
                Message = "An error occurred while updating menu status"
            });
        }
    }

    [Authorize]
    [HttpPatch("{menuId}/meals/{mealId}")]
    public async Task<ActionResult<ApiResponse<MenuMeal>>> UpdateMealDetails(string menuId, string mealId, [FromBody] UpdateMealDetailsDto dto)
    {
        try
        {
            // Get the menu to check its status
            var menu = await _unitOfWork.DailyMenus.GetByIdAsync(menuId);
            if (menu == null)
            {
                return NotFound(new ApiResponse<MenuMeal>
                {
                    Success = false,
                    Message = "Daily menu not found"
                });
            }

            // Check if menu is active (StatusId = 17)
            if (menu.StatusId == 17)
            {
                return BadRequest(new ApiResponse<MenuMeal>
                {
                    Success = false,
                    Message = "Cannot edit meal details while menu is active. Please deactivate the menu first."
                });
            }

            var menuMeal = await _unitOfWork.MenuMeals.GetByIdAsync(mealId);
            if (menuMeal == null || menuMeal.MenuId != menuId)
            {
                return NotFound(new ApiResponse<MenuMeal>
                {
                    Success = false,
                    Message = "Menu meal not found"
                });
            }

            menuMeal.Price = dto.Price;
            menuMeal.AvailableQuantity = dto.AvailibleQuantity;
            menuMeal.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Updating meal {MealId} price to {Price}", mealId, dto.Price);

            await _unitOfWork.MenuMeals.UpdateAsync(menuMeal);

            // Reload with includes
            var updated = await _unitOfWork.MenuMeals.GetAllQueryable()
                .Include(mm => mm.MenuMealRecipes)
                    .ThenInclude(mmr => mmr.Recipe)
                .FirstOrDefaultAsync(mm => mm.Id == mealId);

            return Ok(new ApiResponse<MenuMeal>
            {
                Success = true,
                Data = updated,
                Message = "Meal details updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating meal details {MealId}", mealId);
            return StatusCode(500, new ApiResponse<MenuMeal>
            {
                Success = false,
                Message = "An error occurred while updating meal details"
            });
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<DailyMenu>>> UpdateDailyMenu(string id, [FromBody] DailyMenuDto dto)
    {
        try
        {
            var existing = await _unitOfWork.DailyMenus.GetAllQueryable()
                .Include(m => m.MenuMeals)
                    .ThenInclude(mm => mm.MenuMealRecipes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (existing == null)
            {
                return NotFound(new ApiResponse<DailyMenu>
                {
                    Success = false,
                    Message = "Daily menu not found"
                });
            }

            existing.StatusId = dto.StatusId;
            existing.MenuDate = dto.MenuDate;
            existing.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.DailyMenus.UpdateAsync(existing);

            // Remove old menu meals and their recipes
            foreach (var oldMeal in existing.MenuMeals.ToList())
            {
                foreach (var oldRecipe in oldMeal.MenuMealRecipes.ToList())
                {
                    await _unitOfWork.MenuMealRecipes.DeleteAsync(oldRecipe.MenuMealId, oldRecipe.RecipeId);
                }
                await _unitOfWork.MenuMeals.DeleteAsync(oldMeal.Id);
            }

            // Add new menu meals and their recipes
            foreach (var mealDto in dto.MenuMeals)
            {
                var menuMeal = new MenuMeal
                {
                    Id = mealDto.Id ?? Guid.NewGuid().ToString(),
                    MenuId = existing.Id,
                    MealTypeId = mealDto.MealTypeId,
                    Price = mealDto.Price,
                    AvailableQuantity = mealDto.AvailibleQuantity,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.MenuMeals.AddAsync(menuMeal);

                foreach (var recipeId in mealDto.RecipeIds)
                {
                    var menuMealRecipe = new MenuMealRecipe
                    {
                        MenuMealId = menuMeal.Id,
                        RecipeId = recipeId
                    };
                    await _unitOfWork.MenuMealRecipes.AddAsync(menuMealRecipe);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            // Reload with includes
            var updated = await _unitOfWork.DailyMenus.GetAllQueryable()
                .Include(m => m.MenuMeals)
                    .ThenInclude(mm => mm.MenuMealRecipes)
                        .ThenInclude(mmr => mmr.Recipe)
                .FirstOrDefaultAsync(m => m.Id == id);

            return Ok(new ApiResponse<DailyMenu>
            {
                Success = true,
                Data = updated,
                Message = "Daily menu updated successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating daily menu {Id}", id);
            return StatusCode(500, new ApiResponse<DailyMenu>
            {
                Success = false,
                Message = "An error occurred while updating the daily menu"
            });
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteDailyMenu(string id)
    {
        try
        {
            var menu = await _unitOfWork.DailyMenus.GetAllQueryable()
                .Include(m => m.MenuMeals)
                    .ThenInclude(mm => mm.MenuMealRecipes)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menu == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Daily menu not found"
                });
            }

            // Delete menu meal recipes first
            foreach (var meal in menu.MenuMeals.ToList())
            {
                foreach (var recipe in meal.MenuMealRecipes.ToList())
                {
                    await _unitOfWork.MenuMealRecipes.DeleteAsync(recipe.MenuMealId, recipe.RecipeId);
                }
                await _unitOfWork.MenuMeals.DeleteAsync(meal.Id);
            }

            await _unitOfWork.DailyMenus.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "Daily menu deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting daily menu {Id}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = "An error occurred while deleting the daily menu"
            });
        }
    }
}
