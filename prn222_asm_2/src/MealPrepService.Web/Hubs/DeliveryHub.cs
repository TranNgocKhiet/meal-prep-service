using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MealPrepService.Web.Hubs;

[Authorize]
public class DeliveryHub : Hub
{
    public async Task JoinDeliveryGroup(int deliveryId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Delivery_{deliveryId}");
    }

    public async Task LeaveDeliveryGroup(int deliveryId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Delivery_{deliveryId}");
    }

    public async Task SendDeliveryUpdate(int deliveryId, string status, string location, string message)
    {
        await Clients.Group($"Delivery_{deliveryId}").SendAsync("ReceiveDeliveryUpdate", deliveryId, status, location, message);
    }
}
