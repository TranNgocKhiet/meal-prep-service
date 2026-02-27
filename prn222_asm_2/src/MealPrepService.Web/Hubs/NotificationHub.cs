using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MealPrepService.Web.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public async Task JoinUserGroup(int userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
    }

    public async Task LeaveUserGroup(int userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");
    }

    public async Task SendNotification(int userId, string message, string type)
    {
        await Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", message, type);
    }

    public async Task BroadcastNotification(string message, string type)
    {
        await Clients.All.SendAsync("ReceiveNotification", message, type);
    }
}
