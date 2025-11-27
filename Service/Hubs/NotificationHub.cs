using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Service.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task JoinNotificationGroup(string userId)
        {
            var groupName = $"user_{userId}";
            Console.WriteLine($"[HUB] ConnectionId {Context.ConnectionId} JOIN GROUP: {groupName}");

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        public async Task LeaveNotificationGroup(string userId)
        {
            var groupName = $"user_{userId}";
            Console.WriteLine($"[HUB] ConnectionId {Context.ConnectionId} LEAVE GROUP: {groupName}");

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"[HUB] CONNECTED: ConnectionId = {Context.ConnectionId}");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine($"[HUB] DISCONNECTED: ConnectionId = {Context.ConnectionId}, Reason = {exception?.Message}");

            await base.OnDisconnectedAsync(exception);
        }
    }
}