using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace MealPrepService.Web.Pages.Order;

[Authorize(Roles = "Customer")]
public class PaymentErrorModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Message { get; set; } = string.Empty;

    public void OnGet()
    {
        if (string.IsNullOrEmpty(Message))
        {
            Message = "An error occurred while processing your payment. Please try again or contact support.";
        }
    }
}
