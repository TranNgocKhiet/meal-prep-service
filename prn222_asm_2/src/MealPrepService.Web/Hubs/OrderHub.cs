using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MealPrepService.Web.Hubs;

[Authorize]
public class OrderHub : Hub
{
    public async Task JoinOrderGroup(int orderId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"Order_{orderId}");
    }

    public async Task LeaveOrderGroup(int orderId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Order_{orderId}");
    }

    public async Task SendOrderStatusUpdate(int orderId, string status, string message)
    {
        await Clients.Group($"Order_{orderId}").SendAsync("ReceiveOrderStatusUpdate", orderId, status, message);
    }
}
