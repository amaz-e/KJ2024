namespace MemeBE.Models;

public class Room
{
    public int RoomId { get; set; }
    public Dictionary<string,Player> Players { get; set; }
    public bool Locked { get; set; }
    public Room(int roomID)
    {
        RoomId = roomID;
        Players = new Dictionary<string, Player>(); 
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
}