namespace MemeBE.Models;

public class Room
{
    public string RoomId { get; set; }
    public Dictionary<string,Player> Players { get; set; }
    public bool Locked { get; set; }
    
    //Cards that can be drawn
    public Deck Deck { get; set; }
    // Is game started
    public bool GameStarted { get; set; } = false;
    public string Owner { get; set; }
    public Player ActivePlayer { get; set; }
    public Queue<Player> PlayerQueue { get; set; }
    
    
    public Room(string roomID, string owner)
    {
        RoomId = roomID;
        Owner = owner;
        Players = new Dictionary<string, Player>();

        Deck = new Deck();
    }
    public void InitGame()
    {
        //TODO stworzenie talli
        
        //TODO wygenerowanie kolejki
        Random rng = new Random();
        PlayerQueue = new Queue<Player>(Players.Values.OrderBy(p => rng.Next()));
        //TODO ustawienie aktywnego gracza
        ActivePlayer = NextPlayer();

    }
    public Result AddPlayer(Player player)
    {
        //TODO: Add Validation
        if (Players.Count > 3)
        {
            return Result.Fail("Room is full... find more friends");
        }
        if (ContainsPlayerWithNick(player.Nick))
        {
            return Result.Fail("Player with that nick already in the room... be more original");
        }
        Players.Add(player.ConnectionID, player);
        return Result.Ok("Connected to room");
    }
    
    
    
    public bool ContainsPlayerWithNick(string nick)
    {
        // Check if any player in the dictionary has the specified nick
        return Players.Values.Any(player => player.Nick.Equals(nick, StringComparison.OrdinalIgnoreCase));
    }

    public Player GetPlayerByConnID(string connId)
    {
        return Players.Values.First(player => player.ConnectionID.Equals(connId));
    }


    
    public Player NextPlayer()
    {
        // Dequeue the player from the front of the queue.
        var player = PlayerQueue.Dequeue();

        // Immediately enqueue the player at the back of the queue.
        PlayerQueue.Enqueue(player);

        // Return the player that was processed.
        return player;
    }
}