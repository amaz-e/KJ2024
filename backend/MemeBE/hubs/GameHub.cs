using System.Linq.Expressions;
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
        catch(Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
        Console.WriteLine("SendMessage - sent");
    }
    
    public async Task GetRooms()
    {
        await Clients.All.SendAsync("RoomsList", Groups);
    }

    public async Task CreateRoom(string playerName)
    {
        if (string.IsNullOrEmpty(playerName.Trim()))
        {
            await Clients.Caller.SendAsync("LobbyError", "Pick a better name... or any name at least");
            return;
        }
        // Assign room id and incerement value for the next one
        var currentRoomId = nextRoomID.ToString();
        nextRoomID++;
        
        // Create room instance and add it to dict
        Room room = new Room(currentRoomId,playerName);
        rooms.Add(currentRoomId, room);
        
        //Response
        await JoinRoom(playerName, currentRoomId.ToString());
        Console.WriteLine("Room created by " + playerName + ", room id " + room.RoomId);
    }

    public async Task JoinRoom(string playerName, string roomID)
    {
        if (string.IsNullOrEmpty(playerName.Trim()))
        {
            await Clients.Caller.SendAsync("LobbyError", "Pick a better name... or any name at least");
            return;
        }
        var isRoomCreated = rooms.TryGetValue(roomID, out var room);
        if (!isRoomCreated)
        {
            await Clients.Caller.SendAsync("LobbyError", "Room does not exist... yet");
        }
        else if (room.GameStarted)
        {
            await Clients.Caller.SendAsync("LobbyError", "They started without You... figures");
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
            await Clients.OthersInGroup(roomID).SendAsync("NewPlayerJoinedToRoom", playerName);
        }
    }

    public async Task StartGame()
    {
        
        
         if (!playerRoomMap.TryGetValue(Context.ConnectionId, out var room))
        {
            await Clients.Caller.SendAsync("RoomError", "Failed to start the game... but you are a talented one this error is hard to reach good for You");
        }
         else if (room.Players.Count < 2)
         {
             await Clients.Caller.SendAsync("RoomError", "You want to play with yourself? Perv...");
         }
         else if (room.GameStarted)
         {
             await Clients.Caller.SendAsync("RoomError", "You cannot start what is already in motion...");
         }
        else if (room.Owner.Equals(room.GetPlayerByConnID(Context.ConnectionId).Nick))
        {
            
            room.GameStarted = true;
            room.InitGame();
            
            
            await Clients.Group(room.RoomId).SendAsync("GameStarted");
            
            foreach (var player in room.Players.Values)
            {
                // Zainicjuj gre dla kaźdego gracza
                for (int i = 0; i < 5; i++)
                {
                    DrawCard(player, room);

                }
            }

            NextTurn(room);
        }
        else
        {
            await Clients.Caller.SendAsync("RoomError", "You are not the room owner, budy... how did You get that button ?");
        }

        
    }

    public async Task NextTurn(Room room)
    {
        await Clients.Client(room.ActivePlayer.ConnectionID).SendAsync("TurnStarted");
        await Clients.Group(room.RoomId)
            .SendAsync("ReceiveServerRoomMessage", room.ActivePlayer.Nick + " - turn started");
    }

    public async Task SendCard(int cardId, string targetNick)
    {
        if (!playerRoomMap.TryGetValue(Context.ConnectionId, out var room))
        {
            await Clients.Caller.SendAsync("RoomError", "No no no no - You can't make moves in this room");
        }
        else if (room.ActivePlayer.ConnectionID.Equals(Context.ConnectionId))
        {
            // tutaj jest aktywny gracz w odpowiednim pokoju
            // Wyliczanie akcjki
            var activeCard = room.ActivePlayer.CardsOnHand.SingleOrDefault(card => card.DeckId == cardId);
            Clients.Group(room.RoomId).SendAsync("LastCard", activeCard.URL);

            foreach (var effect in activeCard.EffectList)
            {
                if (activeCard.Target == 0) // Attack actions
                {
                    switch (effect.EffectName)
                    {
                        case "MakeLaugh":
                            MakeLaugh(room, effect.Value);
                            break;
                        case "MakeGrumpy":
                            MakeGrumpy(room, effect.Value);
                            break;
                        default:
                            await Clients.Caller.SendAsync("RoomError", "No no no no - You can't make moves like that");
                            break;
                    }

                    
                    // wyslać info do frontendu eby usunał kartę
                    Clients.Group(room.RoomId).SendAsync("RemoveCard", cardId);
                }
                if (activeCard.Target == 1) // Attack actions
                {
                    switch (effect.EffectName)
                    {
                        case "MakeLaugh":
                            MakeLaugh(room, effect.Value, targetNick);
                            break;
                        case "MakeGrumpy":
                            MakeGrumpy(room, effect.Value, targetNick);
                            break;
                        default:
                            await Clients.Caller.SendAsync("RoomError", "No no no no - You can't make moves like that");
                            break;
                    }
                    
                    // wyslać info do frontendu eby usunał kartę
                    Clients.Group(room.RoomId).SendAsync("RemoveCard", cardId);
                }
                else if (activeCard.Target == 2) // Actions on player persistent slot
                {
                    PlacePersistentCard(room, activeCard, targetNick);

                    // poinformuj wszystkich ze tej karty juz neima 
                    Clients.Group(room.RoomId).SendAsync("RemoveCard", cardId, true);
                }
            }
            room.ActivePlayer.CardsOnHand.Remove(activeCard);
        }
    }
    public async Task EndTurn(Room room)
    {
        Console.WriteLine("End turn - start");
        await Clients.Client(room.ActivePlayer.ConnectionID).SendAsync("TurnEnd");
//         await Clients.Group(room.RoomId)
//             .SendAsync("ReceiveServerRoomMessage", room.ActivePlayer.Nick + " - turn Ended");

        foreach (var player in room.Players.Values)
        {
            if (player.LaughPoints > 14)
            {
                room.GameStarted = false;
            }
        }

        
        if (room.GameStarted)
        {
            DrawCard(room.ActivePlayer, room);
            room.ActivePlayer = room.NextPlayer();
            NextTurn(room);
            
        }
        else
        {
            GameEnded(room);
        }
        Console.WriteLine("######################");
    }
    public async Task DrawCard(Player player, Room room)
    {
        Console.WriteLine("DrawCard " + player.Nick + " room: " +room.RoomId);
        var isDeckEmpty = room.DrawCard(out Card card).Sucess;
        if (!isDeckEmpty)
        {
            GameEnded(room);
        }
        else
        {
            await Clients.Client(player.ConnectionID)
                .SendAsync("CardDrawn", card.DeckId, card.URL, card.Target);
            await Clients.Group(room.RoomId)
                .SendAsync("CardsLeft", room.Deck.GetCount());
            player.AddCardToHand(card);
        }
    }

    public async Task DebugGameEnded(string roomId)
    {
        GameEnded(rooms[roomId]);
    }
    public async Task GameEnded(Room room)
    {
        Console.WriteLine("GameEnd - start");
        var sortedPlayersByPoints = room.Players.Values
            .OrderBy(p => p.LaughPoints)
            .ToList();
        
        var winner = sortedPlayersByPoints.First();
        var loser = sortedPlayersByPoints.Last();

// Generate the message
        var message = $"<p>Kudos to <strong>{winner.Nick}</strong>, the stoic champ with just <strong>{winner.LaughPoints}</strong> points! 🏆</p>" +
                      $"<p>Shoutout to <strong>{loser.Nick}</strong> for the biggest laugh tally of <strong>{loser.LaughPoints}</strong> points! 😄</p>";

// Add the list of all players with their points
        foreach (var player in sortedPlayersByPoints)
        {
            message += $"{player.Nick}: {player.LaughPoints} Laugh Points</br>";
        }

// Output the message
        Console.WriteLine(message);
        
        await Clients.Group(room.RoomId)
            .SendAsync("GameEnded", message);
    }
    public List<Player> GetOtherPlayers(Room room, string connId)
    {
        return room.Players.Values.Where(player => player.ConnectionID != connId).ToList();
    }

    public async Task MakeLaugh(Room room, int value, string? targetNick = null)
    {
        
        int laughValue = room.ActivePlayer.PrepareLaugh(value, out List<int?> cardsToDelete);
        if (string.IsNullOrEmpty(targetNick)) // all players
        {
            Console.WriteLine("Make laugh to all");
            foreach (var player in room.Players.Values)
            {
                Console.WriteLine("Player " +player.Nick +" is gone take laugh: " + laughValue);
                player.ReceiveLaugh(laughValue,out List<int?> cardsToDeleteFromShieldBuff);
                // Emit
                await Clients.Client(player.ConnectionID).SendAsync("TakeLaugh",player.LaughPoints);
                var otherPlayers = GetOtherPlayers(room, player.ConnectionID);
                foreach (var otherPlayer in otherPlayers)  
                {
                    await Clients.Client(otherPlayer.ConnectionID).SendAsync("OtherTookLaugh",player.Nick, player.LaughPoints);
                }
                await Clients.Group(room.RoomId)
                    .SendAsync("ReceiveServerRoomMessage", Helpers.GetLaughMessage(player.Nick, laughValue));
                foreach (var cardId in cardsToDeleteFromShieldBuff)
                {
                    Clients.Group(room.RoomId).SendAsync("RemoveCard", cardId);
                }
            }
        }
        else // Single player
        {
            Console.WriteLine("Make laugh to " + targetNick);
            var targetPlayer = room.GetPlayerByNick(targetNick);
            Console.WriteLine("Player " +targetPlayer.Nick +" is gone take laugh: " + laughValue);
            targetPlayer.ReceiveLaugh(laughValue, out List<int?> cardsToDeleteFromShieldBuff);
            // emit
            await Clients.Client(targetPlayer.ConnectionID).SendAsync("TakeLaugh",targetPlayer.LaughPoints);
            var otherPlayers = GetOtherPlayers(room, targetPlayer.ConnectionID);
            foreach (var otherPlayer in otherPlayers)  
            {
                await Clients.Client(otherPlayer.ConnectionID).SendAsync("OtherTookLaugh",targetPlayer.Nick, targetPlayer.LaughPoints);
            }
            await Clients.Group(room.RoomId)
                .SendAsync("ReceiveServerRoomMessage", Helpers.GetLaughMessage(targetPlayer.Nick, laughValue));
            foreach (var cardId in cardsToDeleteFromShieldBuff)
            {
                Clients.Group(room.RoomId).SendAsync("RemoveCard", cardId);
            }
        }

        foreach (var cardId in cardsToDelete)
        {
            Clients.Group(room.RoomId).SendAsync("RemoveCard", cardId);
        }
        
        EndTurn(room);
    }


    public async Task MakeGrumpy(Room room, int value, string? targetNick = null)
    {
        Console.WriteLine("MakeGrumpy - invoke");
        int grumpyValue = room.ActivePlayer.PrepareGrumpy(value, out List<int?> cardsToDelete);
        Console.WriteLine("Grumpy Value: " + grumpyValue);
        if (string.IsNullOrEmpty(targetNick)) // all players
        {
            Console.WriteLine("MakeGrumpy - To All");
            foreach (var player in room.Players.Values)
            {
                Console.WriteLine("Player " +player.Nick +" is gone take grumpy: " + grumpyValue);
                player.ReceiveGrumpy(grumpyValue, out List<int?> cardsToDeleteFromShieldBuff);
                // Emit
                await Clients.Client(player.ConnectionID).SendAsync("TakeGrumpy",player.LaughPoints);
                var otherPlayers = GetOtherPlayers(room, player.ConnectionID);
                foreach (var otherPlayer in otherPlayers)  
                {
                    await Clients.Client(otherPlayer.ConnectionID).SendAsync("OtherTookGrumpy",player.Nick, player.LaughPoints);
                }
                await Clients.Group(room.RoomId)
                    .SendAsync("ReceiveServerRoomMessage", Helpers.GetGrumpyMessage(player.Nick, grumpyValue));
                foreach (var cardId in cardsToDeleteFromShieldBuff)
                {
                    Clients.Group(room.RoomId).SendAsync("RemoveCard", cardId);
                }
            }
        }
        else // Single player
        { 
            Console.WriteLine("Player " +targetNick +" is gone take grumpy " + grumpyValue);
            var targetPlayer = room.GetPlayerByNick(targetNick);
            targetPlayer.ReceiveGrumpy(grumpyValue,out List<int?> cardsToDeleteFromShieldBuff);
            // emit
            await Clients.Client(targetPlayer.ConnectionID).SendAsync("TakeGrumpy",targetPlayer.LaughPoints);
            var otherPlayers = GetOtherPlayers(room, targetPlayer.ConnectionID);
            foreach (var otherPlayer in otherPlayers)  
            {
                await Clients.Client(otherPlayer.ConnectionID).SendAsync("OtherTookGrumpy",targetPlayer.Nick, targetPlayer.LaughPoints);
            }
            await Clients.Group(room.RoomId)
                .SendAsync("ReceiveServerRoomMessage", Helpers.GetGrumpyMessage(targetPlayer.Nick, grumpyValue));
            foreach (var cardId in cardsToDeleteFromShieldBuff)
            {
                Clients.Group(room.RoomId).SendAsync("RemoveCard", cardId);
            }
        }
        foreach (var cardId in cardsToDelete)
        {
            Clients.Group(room.RoomId).SendAsync("RemoveCard", cardId);
        }
        EndTurn(room);
    }

    public async Task PlacePersistentCard(Room room, Card card, String targetNick)
    {
        Console.WriteLine("Place persistant to "+ targetNick);
        var targetPlayer = room.GetPlayerByNick(targetNick);
        if (targetPlayer.AddCardToPersistentSlot(card).Sucess)
        {
            await Clients.Client(targetPlayer.ConnectionID).SendAsync("PlacedPersistent",card.DeckId, card.URL, card.Target);
            var otherPlayers = GetOtherPlayers(room, targetPlayer.ConnectionID).Select(player => player.Nick).ToList();
            foreach (var nick in otherPlayers)
            {
                await Clients.Client(room.GetPlayerByNick(nick).ConnectionID).SendAsync("OtherPlacedPersistent",card.DeckId, card.URL, targetNick);
            }
            await Clients.Group(room.RoomId)
                .SendAsync("ReceiveServerRoomMessage", Helpers.GetPersistantMessage(targetPlayer.Nick, card.EffectList[0].EffectName));
        }
        EndTurn(room);
    }


    // Target = 0 - all
    // 1 - player
    // 2 - persistent slot
}