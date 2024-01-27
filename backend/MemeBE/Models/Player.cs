namespace MemeBE.Models;

public class Player
{
    public string ConnectionID { get; set; }
    public string Nick { get; set; }
    
    
    public Player(string connectionId, string nick)
    {
        ConnectionID = connectionId;
        Nick = nick;
    }
}