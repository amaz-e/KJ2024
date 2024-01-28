namespace MemeBE.Models;

public class Deck
{
    private Queue<Card> deckCards { get; set; }
    
    
    public Deck()
    {
        deckCards = PrepareDeckCards(Helpers.Cards);
    }

    private Queue<Card> PrepareDeckCards(List<Card> cards)
    {
        int deckId = 1;
        Queue<Card> result = new Queue<Card>();
        foreach (var card in cards)
        {
            card.DeckId = deckId;
            deckId++;
            result.Enqueue(card);
        }
        
        Random rng = new Random();
        result = new Queue<Card>(cards.OrderBy(p => rng.Next()));
        
        return result;
    }

    public int GetCount()
    {
        return deckCards.Count();
    }
    public Card? DrawCard()
    {
        return deckCards.Dequeue();
    }
    
    
}