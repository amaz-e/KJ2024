using Microsoft.AspNetCore.SignalR;

namespace MemeBE.hubs;

public class GameHub : Hub
{
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }

    public async Task CreateRoom(string PlayerName)
    {
        
    }

    public async Task JoinRoom(string PlayerName)
    {
        
    }
}