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
                //TODO Zainicjuj gre dla kaźdego gracza
                for (int i = 0; i < 5; i++)
                {
                    CardDrawn(player, room);

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
        else if (room.ActivePlayer.ConnectionID.Equals(room.GetPlayerByConnID(Context.ConnectionId).Nick))
        {
            // tutaj jest aktywny gracz w odpowiednim pokoju
            // Wyliczanie akcjki
            var ActiveCard = room.ActivePlayer.CardsOnHand.SingleOrDefault(card => card.DeckId == cardId);

            foreach (var effect in ActiveCard.EffectList)
            {
                if (ActiveCard.Target == 0) // Attack actions
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
                }
                if (ActiveCard.Target == 1) // Attack actions
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
                }
                else if (ActiveCard.Target == 2) // Actions on player persistent slot
                {
                    PlacePersistentCard(room, ActiveCard, targetNick);
                    
                }
            }
        }
    }
    public async Task EndTurn(Room room)
    {
        await Clients.Client(room.ActivePlayer.ConnectionID).SendAsync("TurnEnd");
        await Clients.Group(room.RoomId)
            .SendAsync("ReceiveServerRoomMessage", room.ActivePlayer.Nick + " - turn Ended");
        
        if (room.GameStarted)
        {
            room.ActivePlayer = room.NextPlayer();
            NextTurn(room);
        }
        else
        {
            GameEnded(room);
        }
    }
    public async Task CardDrawn(Player player, Room room)
    {
        var isDeckEmpty = room.DrawCard(out Card card).Sucess;
        if (!isDeckEmpty)
        {
            GameEnded(room);
        }
        else
        {
            await Clients.Client(player.ConnectionID)
                .SendAsync("CardDrawn", card.DeckId, card.URL, card.Target);
            player.AddCardToHand(card);
        }
    }

    public async Task GameEnded(Room room)
    {
        // Oblicz osteateczne wartosci i przekaz do frontu
        await Clients.Group(room.RoomId)
            .SendAsync("GameEnded");
    }
    public List<Player> GetOtherPlayers(Room room, string connId)
    {
        return room.Players.Values.Where(player => player.ConnectionID != connId).ToList();
    }

    public async Task MakeLaugh(Room room, int value, string? targetNick = null)
    {
        int laughValue = room.ActivePlayer.PrepareLaugh(value);
        if (string.IsNullOrEmpty(targetNick)) // all players
        {
            foreach (var player in room.Players.Values)
            {
                player.ReceiveLaugh(laughValue);
                // Emit
                await Clients.Client(player.ConnectionID).SendAsync("TakeLaugh",player.LaughPoints);
                await Clients.OthersInGroup(room.RoomId).SendAsync("OtherTookLaugh",player.Nick, player.LaughPoints);
                await Clients.Group(room.RoomId)
                    .SendAsync("ReceiveServerRoomMessage", GetLaughMessage(player.Nick, laughValue));
            }
            
        }
        else // Single player
        {
            var targetPlayer = room.GetPlayerByNick(targetNick);
            targetPlayer.ReceiveLaugh(laughValue);
            // emit
            await Clients.Client(targetPlayer.ConnectionID).SendAsync("TakeLaugh",targetPlayer.LaughPoints);
            await Clients.OthersInGroup(room.RoomId).SendAsync("OtherTookLaugh",targetPlayer.Nick, targetPlayer.LaughPoints);
            await Clients.Group(room.RoomId)
                .SendAsync("ReceiveServerRoomMessage", GetLaughMessage(targetPlayer.Nick, laughValue));
        }
        
    }


    public async Task MakeGrumpy(Room room, int value, string? targetNick = null)
    {
        int grumpyValue = room.ActivePlayer.PrepareGrumpy(value);
        if (string.IsNullOrEmpty(targetNick)) // all players
        {
            foreach (var player in room.Players.Values)
            {
                player.ReceiveGrumpy(grumpyValue);
                // Emit
                await Clients.Client(player.ConnectionID).SendAsync("TakeGrumpy",player.LaughPoints);
                await Clients.OthersInGroup(room.RoomId).SendAsync("OtherTookGrumpy",player.Nick, player.LaughPoints);
                await Clients.Group(room.RoomId)
                    .SendAsync("ReceiveServerRoomMessage", GetGrumpyMessage(player.Nick, grumpyValue));
            }
        }
        else // Single player
        { 
            var targetPlayer = room.GetPlayerByNick(targetNick);
            targetPlayer.ReceiveGrumpy(grumpyValue);
            // emit
            await Clients.Client(targetPlayer.ConnectionID).SendAsync("TakeGrumpy",targetPlayer.LaughPoints);
            await Clients.OthersInGroup(room.RoomId).SendAsync("OtherTookLaugh",targetPlayer.Nick, targetPlayer.LaughPoints);
            await Clients.Group(room.RoomId)
                .SendAsync("ReceiveServerRoomMessage", GetGrumpyMessage(targetPlayer.Nick, grumpyValue));
        }
    }

    public void PlacePersistentCard(Room room, Card card, String targetNick)
    {
        var player = room.GetPlayerByNick(targetNick);
        if (player.AddCardToPersistentSlot(card).Sucess)
        {
            //emit   
        }
    }
    private string GetLaughMessage(string playerNick, int laughValue)
    {
        Random rnd = new Random();
        string[] laughMessages = new string[]
        {
            "Hold onto your funny bones! " + playerNick + " has just boosted the laugh-o-meter with an extra " +
            laughValue + " HAHA points!",
            "Alert! " + playerNick + " is on a chuckle charge, racking up another " + laughValue +
            " HAHA points to their giggle bank!",
            "Whoa, " + playerNick + " is serving up some serious hilarity with an additional " + laughValue +
            " HAHA points! Comedy gold!",
            "What's that sound? It's " + playerNick + " laughing all the way to the HAHA hall of fame with " +
            laughValue + " more points of pure mirth!",
            "Breaking news: " + playerNick + "'s laughter level just skyrocketed by " + laughValue +
            " HAHA points. The fun-o-sphere is off the charts!",
            "Gather 'round, folks! Witness " + playerNick + " sprinkle a dash of chuckles, adding " + laughValue +
            " HAHA points to their giggle tally!",
            "It's no joke! " + playerNick + " is officially a laughter legend, piling on an extra " + laughValue +
            " HAHA points to their chuckle stash!",
            "Behold! " + playerNick + " has unleashed a laughnado, swirling up " + laughValue +
            " HAHA points in a frenzy of funniness!",
            "Can you feel the ground shaking? That's just " + playerNick + " laughing up an earthquake with another " +
            laughValue + " HAHA points on the Richter scale!",
            "Quick, get the giggle goggles! " + playerNick + " is blinding us with brilliance, bagging " + laughValue +
            " more HAHA points in a spectacular show of snickers!"
        };

        int randomIndex = rnd.Next(laughMessages.Length);
        return laughMessages[randomIndex];
    }
    private string GetGrumpyMessage(string playerNick, int grumpyValue)
    {
        Random rnd = new Random();
        string[] grumpyMessages = new string[]
        {
            "Uh oh! " + playerNick + " just unleashed a grump-storm, draining " + grumpyValue + " joy points from the room. Cheer up, buttercup!",
            "Alert the mood squad! " + playerNick + " is having a no-smiles spell, dropping " + grumpyValue + " happiness points like hot potatoes!",
            "Gloomy alert! " + playerNick + " has just cast a little cloud of frowns, sapping " + grumpyValue + " cheer points from the happiness meter!",
            "Who turned down the joy tunes? " + playerNick + " has, with a grumpyValue-sized dip in the vibe vibes!",
            "Hold the chuckles, " + playerNick + " is on a serious spree, swiping " + grumpyValue + " giggle points from the laugh ledger!",
            "Warning! " + playerNick + "'s smile seems to be on a brief break, taking " + grumpyValue + " glee points with it!",
            "Looks like " + playerNick + " hit a pothole on the joyride, causing a loss of " + grumpyValue + " happy points. Time for a pick-me-up!",
            "Is it just me, or did " + playerNick + " just start a frown-a-thon? There go " + grumpyValue + " points from the joy jar!",
            "Notice: " + playerNick + "’s happiness meter just experienced a slight dip, with a " + grumpyValue + " drop in delight digits!",
            "Eek! " + playerNick + " just tripped on a grumble bump, shedding " + grumpyValue + " bliss bits off their happiness score!"
        };

        int randomIndex = rnd.Next(grumpyMessages.Length);
        return grumpyMessages[randomIndex];
    }

    // Target = 0 - all
    // 1 - player
    // 2 - persistent slot
}