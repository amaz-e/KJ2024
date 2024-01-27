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
                
        //CHANGEME 
        //TODO Transformacja tabeli kart na DECK do gry
        // Dodaj ilosci odpowiednich kart
        // Szufluj
        return new Queue<Card>(cards);
    }

    public Card? DrawCard()
    {
        return deckCards.Dequeue();
    }
    
    
}