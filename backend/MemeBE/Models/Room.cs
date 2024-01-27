namespace MemeBE.Models;

public class Room
{
    public int RoomId { get; set; }
    public Dictionary<string,Player> Players { get; set; }
    public Room(int roomID)
    {
        RoomId = roomID;
    }
}