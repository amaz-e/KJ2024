namespace MemeBE.Models;

public class Player
{
    public string ConnectionID { get; set; }
    public string Nick { get; set; }
    
    public List<Card> CardsOnHand { get; set; }
    public List<Card> PersistentSlot { get; set; }
    
    public Player(string connectionId, string nick)
    {
        ConnectionID = connectionId;
        Nick = nick;
    }

    public void AddCardToHand(Card card)
    {
        CardsOnHand.Add(card);
    }

    public Result AddCardToPersistentSlot(Card card)
    {
        if (PersistentSlot.Count >= 3)
        {
            return Result.Fail("Slots full, throw something out");
        }
        PersistentSlot.Add(card);
        return Result.Ok();
    }
}