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

    public int PrepareLaugh(int value)
    {
        int result = value;
        foreach (var card in PersistentSlot)
        {
            foreach (Effect effect in card.EffectList)
            {
                if (effect.EffectName.Equals("Buff"))
                {
                    result += effect.Value;
                }
                
            }
        }
        
        
        return result;
    }

    public void ReceiveLaugh(int laughValue)
    {
        var laughReceived = laughValue;
        var cardsCopy = new List<Card>(PersistentSlot);
        PersistentSlot.Clear();
        foreach (var card in cardsCopy)
        {
            foreach (Effect effect in card.EffectList)
            {
                if (effect.EffectName.Equals("Shield"))
                {
                    laughReceived -= effect.Value;
                }
                else
                {
                    PersistentSlot.Add(card);
                }
                
            }
        }
        laughReceived = laughReceived < 0 ? 0 : laughReceived;
    }

    public int PrepareGrumpy(int value)
    {
        int result = value;
        foreach (var card in PersistentSlot)
        {
            foreach (Effect effect in card.EffectList)
            {
                if (effect.EffectName.Equals("Buff"))
                {
                    result += effect.Value;
                }
                
            }
        }
        
        return result;
    }

    public void ReceiveGrumpy(int grumpyValue)
    {
        var grumpyReceived = grumpyValue;
        var cardsCopy = new List<Card>(PersistentSlot);
        PersistentSlot.Clear();
        foreach (var card in cardsCopy)
        {
            foreach (Effect effect in card.EffectList)
            {
                if (effect.EffectName.Equals("Shield"))
                {
                    grumpyReceived -= effect.Value;
                }
                else
                {
                    PersistentSlot.Add(card);
                }
                
            }
        }
        grumpyReceived = grumpyReceived < 0 ? 0 : grumpyReceived;
    }
}