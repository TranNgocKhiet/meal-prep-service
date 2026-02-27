using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace MealPrepService.Web.Pages.PublicMenu;

[AllowAnonymous]
public class NextWeekModel : PageModel
{
    public IActionResult OnGet(DateTime? currentWeekStart)
    {
        var weekStart = currentWeekStart ?? GetStartOfWeek(DateTime.Today);
        var nextWeekStart = weekStart.AddDays(7);
        return RedirectToPage("/PublicMenu/Weekly", new { startDate = nextWeekStart });
    }

    private DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Sunday)) % 7;
        return date.AddDays(-1 * diff).Date;
    }
}
