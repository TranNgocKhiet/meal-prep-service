using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MealPrepService.Web.Hubs;

[Authorize]
public class OrderHub : Hub
{
    public async Task JoinOrderGroup(string orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Order_{orderId}");
    }

    public async Task LeaveOrderGroup(string orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Order_{orderId}");
    }

    public async Task SendOrderStatusUpdate(string orderId, string status, string message)
    {
        await Clients.Group($"Order_{orderId}").SendAsync("ReceiveOrderStatusUpdate", orderId, status, message);
    }
}
