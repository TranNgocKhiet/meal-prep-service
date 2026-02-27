using Microsoft.AspNetCore.SignalR;

namespace MealPrepService.Web.Hubs;

public class MenuHub : Hub
{
    public async Task SendMenuUpdate(DateTime menuDate, int mealId, int quantityRemaining)
    {
        await Clients.All.SendAsync("ReceiveMenuUpdate", menuDate, mealId, quantityRemaining);
    }

    public async Task SendMenuAvailabilityAlert(DateTime menuDate, int mealId, string mealName, bool isAvailable)
    {
        await Clients.All.SendAsync("ReceiveMenuAvailabilityAlert", menuDate, mealId, mealName, isAvailable);
    }
    
    public async Task SendMenuStatusChange(Guid menuId, DateTime menuDate, bool isActive)
    {
        await Clients.All.SendAsync("ReceiveMenuStatusChange", menuId.ToString(), menuDate.ToString("yyyy-MM-dd"), isActive);
    }
}
