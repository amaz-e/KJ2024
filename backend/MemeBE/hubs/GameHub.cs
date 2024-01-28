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
        await Clients.Group(room.RoomId)
            .SendAsync("ReceiveServerRoomMessage", room.ActivePlayer.Nick + " - turn Ended");
        
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
            player.AddCardToHand(card);
        }
    }

    public async Task GameEnded(Room room)
    {
        Console.WriteLine("GameEnd - start");
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
        int laughValue = room.ActivePlayer.PrepareLaugh(value, out List<int?> cardsToDelete);
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

        foreach (var cardId in cardsToDelete)
        {
            Clients.Group(room.RoomId).SendAsync("RemoveCard", cardId);
        }
        EndTurn(room);
    }


    public async Task MakeGrumpy(Room room, int value, string? targetNick = null)
    {
        int grumpyValue = room.ActivePlayer.PrepareGrumpy(value, out List<int?> cardsToDelete);
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
        foreach (var cardId in cardsToDelete)
        {
            Clients.Group(room.RoomId).SendAsync("RemoveCard", cardId);
        }
        EndTurn(room);
    }

    public async Task PlacePersistentCard(Room room, Card card, String targetNick)
    {
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
                .SendAsync("ReceiveServerRoomMessage", GetPersistantMessage(targetPlayer.Nick, card.EffectList[0].EffectName));
        }
        EndTurn(room);
    }
    private string GetLaughMessage(string playerNick, int laughValue)
    {
        Random rnd = new Random();
        string[] laughMessages = new string[]
        {
            "Hold onto your funny bones! <b>" + playerNick + "</b> has just boosted the laugh-o-meter with an extra <b>" +
            laughValue + "</b> HAHA points!",
            "Alert! <b>" + playerNick + "</b> is on a chuckle charge, racking up another <b>" + laughValue +
            "</b> HAHA points to their giggle bank!",
            "Whoa, <b>" + playerNick + "</b> is serving up some serious hilarity with an additional <b>" + laughValue +
            "</b> HAHA points! Comedy gold!",
            "What's that sound? It's <b>" + playerNick + "</b> laughing all the way to the HAHA hall of fame with <b>" +
            laughValue + "</b> more points of pure mirth!",
            "Breaking news: <b>" + playerNick + "</b>'s laughter level just skyrocketed by <b>" + laughValue +
            "</b> HAHA points. The fun-o-sphere is off the charts!",
            "Gather 'round, folks! Witness <b>" + playerNick + "</b> sprinkle a dash of chuckles, adding <b>" + laughValue +
            "</b> HAHA points to their giggle tally!",
            "It's no joke! <b>" + playerNick + "</b> is officially a laughter legend, piling on an extra <b>" + laughValue +
            "</b> HAHA points to their chuckle stash!",
            "Behold! <b>" + playerNick + "</b> has unleashed a laughnado, swirling up <b>" + laughValue +
            "</b> HAHA points in a frenzy of funniness!",
            "Can you feel the ground shaking? That's just <b>" + playerNick + "</b> laughing up an earthquake with another <b>" +
            laughValue + "</b> HAHA points on the Richter scale!",
            "Quick, get the giggle goggles! <b>" + playerNick + "</b> is blinding us with brilliance, bagging <b>" + laughValue +
            "</b> more HAHA points in a spectacular show of snickers!"
        };

        int randomIndex = rnd.Next(laughMessages.Length);
        return laughMessages[randomIndex];
    }
    private string GetGrumpyMessage(string playerNick, int grumpyValue)
    {
        Random rnd = new Random();
        string[] grumpyMessages = new string[]
        {
            "Uh oh! <b>" + playerNick + "</b> just unleashed a grump-storm, draining <b>" + grumpyValue + "</b> joy points from the room. Cheer up, buttercup!",
            "Alert the mood squad! <b>" + playerNick + "</b> is having a no-smiles spell, dropping <b>" + grumpyValue + "</b> happiness points like hot potatoes!",
            "Gloomy alert! <b>" + playerNick + "</b> has just cast a little cloud of frowns, sapping <b>" + grumpyValue + "</b> cheer points from the happiness meter!",
            "Who turned down the joy tunes? <b>" + playerNick + "</b> has, with a grumpyValue-sized dip in the vibe vibes!",
            "Hold the chuckles, <b>" + playerNick + "</b> is on a serious spree, swiping <b>" + grumpyValue + "</b> giggle points from the laugh ledger!",
            "Warning! <b>" + playerNick + "</b>'s smile seems to be on a brief break, taking <b>" + grumpyValue + "</b> glee points with it!",
            "Looks like <b>" + playerNick + "</b> hit a pothole on the joyride, causing a loss of <b>" + grumpyValue + "</b> happy points. Time for a pick-me-up!",
            "Is it just me, or did <b>" + playerNick + "</b> just start a frown-a-thon? There go <b>" + grumpyValue + "</b> points from the joy jar!",
            "Notice: <b>" + playerNick + "</b>’s happiness meter just experienced a slight dip, with a <b>" + grumpyValue + "</b> drop in delight digits!",
            "Eek! <b>" + playerNick + "</b> just tripped on a grumble bump, shedding <b>" + grumpyValue + "</b> bliss bits off their happiness score!"
        };

        int randomIndex = rnd.Next(grumpyMessages.Length);
        return grumpyMessages[randomIndex];
    }
    
    private string GetPersistantMessage(string playerNick, string effectName)
    {
        Random rnd = new Random();
        string[] persistantMessage = new string[]{playerNick + " Did something completely different"};
        if (effectName.Equals("Buff"))
        {
            persistantMessage = new string[]
            {
                "Watch out! <b>" + playerNick +
                "</b> just played a buff card, bolstering their strength for the next round.",
                "Keep your eyes peeled! <b>" + playerNick +
                "</b> has just set up a powerful buff, enhancing their capabilities.",
                "Brace yourselves, <b>" + playerNick + "</b> boosts their arsenal with a well-timed buff card.",
                "Calculating... <b>" + playerNick +
                "</b> plays a buff card, and their power grows silently but surely.",
                "Anticipation builds as <b>" + playerNick +
                "</b> activates a buff card, their strength quietly surging.",
                "Subtle yet effective, <b>" + playerNick + "</b> places a buff card, setting the stage for a comeback.",
                "Poised and ready, <b>" + playerNick + "</b> empowers themselves with a game-changing buff card.",
                "Strength in stillness, <b>" + playerNick + "</b> has played a buff card, their power lying in wait.",
                "Quiet before the storm, <b>" + playerNick +
                "</b> lays down a buff card, their potential energy growing.",
                "Ready for the future, <b>" + playerNick + "</b> buffs up, laying the groundwork for victory."
            };
        }
        else if(effectName.Equals("Shield"))

        {
            persistantMessage = new string[]
            {
                "Strategic mastery! <b>" + playerNick + "</b> fortifies their defenses with an impenetrable shield card.",
                "Shrewd move by <b>" + playerNick + "</b> as they deploy a shield card, preparing for whatever comes next!",
                "With a flick of the wrist, <b>" + playerNick + "</b> lays down a shield card, warding off future attacks.",
                "The tides may turn! <b>" + playerNick + "</b> casts a shield, raising their defenses to new heights.",
                "A moment of peace as <b>" + playerNick + "</b> reinforces their position with a sturdy shield card.",
                "Defensive stance! <b>" + playerNick + "</b> has played a shield card, securing their ground.",
                "An aura of protection envelops <b>" + playerNick + "</b> as they play a strategic shield card.",
                "<b>" + playerNick + "</b> casts a shield, cloaking themselves in an invisible but unbreakable barrier.",
                "A shield card enters the field! <b>" + playerNick + "</b> is now shielded against the next onslaught.",
                "Defense is the best offense! <b>" + playerNick + "</b> places a shield card with a knowing smile."

            };
        }

        int randomIndex = rnd.Next(persistantMessage.Length);
        return persistantMessage[randomIndex];
    }

    // Target = 0 - all
    // 1 - player
    // 2 - persistent slot
}