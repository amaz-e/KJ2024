using System.Threading.Tasks;
using MemeBE.Models;
using Microsoft.AspNetCore.SignalR;

namespace MemeBE.hubs;

public class GameHub : Hub
{
    private static int nextRoomID = 1;
    private static Dictionary<string,Room> rooms = new ();
    private static Dictionary<string, Room> playerRoomMap = new();

    public GameHub()
    {
        try
        {
            Helpers.ParseCardsFromCSV();
        }
        catch
        {
            Console.WriteLine("CSV NOT FOUND");
        }
    }
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
        var currentRoomId = nextRoomID.ToString();
        nextRoomID++;
        
        // Create room instance and add it to dict
        Room room = new Room(currentRoomId,playerName);
        rooms.Add(currentRoomId, room);
        
        //Response
        await JoinRoom(playerName, currentRoomId.ToString());
    }

    public async Task JoinRoom(string playerName, string roomID)
    {
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
            
            playerRoomMap.Add(player.ConnectionID, room);
            
            await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomId);
            await Clients.Caller.SendAsync("JoinedToRoom", room.RoomId, room.Owner.Equals(playerName), GetOtherPlayers(room, player.ConnectionID).Select(player => player.Nick).ToList());
            await Clients.OthersInGroup(roomID.ToString()).SendAsync("NewPlayerJoinedToRoom", playerName);
        }
    }

    public async Task StartGame()
    {
        if (!playerRoomMap.TryGetValue(Context.ConnectionId, out var room))
        {
            await Clients.Caller.SendAsync("RoomError", "Failed to start the game... but you are a talented one this error is hard to reach good for You");
        }
        else if (room.Owner.Equals(room.GetPlayerByConnID(Context.ConnectionId).Nick))
        {
            
            room.GameStarted = true;
            room.InitGame();
            
            await Clients.Group(room.RoomId).SendAsync("GameStarted");
            
            foreach (var player in room.Players)
            {
                //TODO Zainicjuj gre dla kaźdego gracza
                //player.DrawInitCards();
                

            }
            
        }
        else
        {
            await Clients.Caller.SendAsync("RoomError", "You are not the room owner, budy... how did You get that button ?");
        }

        
    }
    public List<Player> GetOtherPlayers(Room room, string connId)
    {
        return room.Players.Values.Where(player => player.ConnectionID != connId).ToList();
    }
}