using System.Threading.Tasks;
using MemeBE.Models;
using Microsoft.AspNetCore.SignalR;

namespace MemeBE.hubs;

public class GameHub : Hub
{
    private static int nextRoomID = 1;
    private static Dictionary<int,Room> rooms = new ();
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }
    
    public async Task GetRooms()
    {
        await Clients.All.SendAsync("RoomsList", Groups);
    }

    public async Task CreateRoom(string playerName)
    {
        // Assign room id and incerement value for the next one
        var currentRoomId = nextRoomID;
        nextRoomID++;
        
        // Create room instance and add it to dict
        Room room = new Room(currentRoomId);
        rooms.Add(currentRoomId, room);
        
        //Response
        await JoinRoom(playerName, currentRoomId.ToString());
    }

    public async Task JoinRoom(string playerName, string roomIDstr)
    {
        var roomID = int.Parse(roomIDstr);

        var isRoomCreated = rooms.TryGetValue(roomID, out var room);
        if (!isRoomCreated)
        {
            await Clients.Caller.SendAsync("LobbyError", "Room does not exist... yet");
        }
        else
        {
            var player = new Player(Context.ConnectionId, playerName);
            var result = room.AddPlayer(player);

            if (!result.Sucess)
            {
                await Clients.Caller.SendAsync("LobbyError", result.Message);
                return;
            }
            
            await Groups.AddToGroupAsync(Context.ConnectionId, nextRoomID.ToString());
            await Clients.Caller.SendAsync("JoinedToRoom", nextRoomID);
            await Clients.OthersInGroup(roomID.ToString()).SendAsync("NewPlayerJoinedToRoom", playerName);
        }
    }
}