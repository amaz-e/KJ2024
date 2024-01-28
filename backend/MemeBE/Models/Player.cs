namespace MemeBE.Models;

public class Player
{
    public string ConnectionID { get; set; }
    public string Nick { get; set; }

    public List<Card> CardsOnHand { get; set; } = new List<Card>();
    public List<Card> PersistentSlot { get; set; } = new List<Card>();

    public int LaughPoints { get; set; }
    
    public Player(string connectionId, string nick)
    {
        ConnectionID = connectionId;
        Nick = nick;
        LaughPoints = 0;
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

    public int PrepareLaugh(int value, out List<int?> cardsToDelete)
    {
        int result = value;
        cardsToDelete = new List<int?>();
        var cardsCopy = new List<Card>(PersistentSlot);
        PersistentSlot.Clear();
        foreach (var card in cardsCopy)
        {
            foreach (Effect effect in card.EffectList)
            {
                if (effect.EffectName.Equals("Buff"))
                {
                    result += effect.Value;
                    cardsToDelete.Add(card.DeckId);
                }
                else
                {
                    PersistentSlot.Add(card);
                }
                
            }
        }
        
        
        return result;
    }

    public int PrepareGrumpy(int value, out List<int?> cardsToDelete)
    {
        int result = value;
        cardsToDelete = new List<int?>();
        var cardsCopy = new List<Card>(PersistentSlot);
        PersistentSlot.Clear();
        foreach (var card in cardsCopy)
        {
            foreach (Effect effect in card.EffectList)
            {
                if (effect.EffectName.Equals("Buff"))
                {
                    result += effect.Value;
                    cardsToDelete.Add(card.DeckId);
                }
                else
                {
                    PersistentSlot.Add(card);
                }

            }
        }

        return result;
    }

    public void ReceiveLaugh(int laughValue,out List<int?> cardsToDelete)
    {
        var laughReceived = laughValue;
        cardsToDelete = new List<int?>();
        var cardsCopy = new List<Card>(PersistentSlot);
        PersistentSlot.Clear();
        foreach (var card in cardsCopy)
        {
            foreach (Effect effect in card.EffectList)
            {
                if (effect.EffectName.Equals("Shield"))
                {
                    Console.WriteLine("Shiled used laugh");
                    laughReceived -= effect.Value;
                    cardsToDelete.Add(card.DeckId);
                }
                else
                {
                    PersistentSlot.Add(card);
                }
                
            }
        }
        laughReceived = laughReceived < 0 ? 0 : laughReceived;
        LaughPoints += laughReceived;

    }

    public void ReceiveGrumpy(int grumpyValue,out List<int?> cardsToDelete)
    {
        var grumpyReceived = grumpyValue;
        cardsToDelete = new List<int?>();
        var cardsCopy = new List<Card>(PersistentSlot);
        PersistentSlot.Clear();
        foreach (var card in cardsCopy)
        {
            foreach (Effect effect in card.EffectList)
            {
                if (effect.EffectName.Equals("Shield"))
                {
                    Console.WriteLine("Shiled used - Grumpy");
                    grumpyReceived -= effect.Value;
                    cardsToDelete.Add(card.DeckId);
                }
                else
                {
                    PersistentSlot.Add(card);
                }
                
            }
        }

        if (grumpyReceived < 0)
        {
            grumpyReceived = 0;
        }
        LaughPoints -= grumpyReceived;
        if (LaughPoints < 0)
        {
            LaughPoints = 0;
        }
    }
}