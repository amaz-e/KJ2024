namespace MemeBE.Models;

public class Deck
{
    private List<Card> deckCards { get; set; }
    
    private List<Card> cardTable;
    

    public Deck()
    {
        cardTable = Helpers.Cards;

        //TODO Transformacja tabeli kart na DECK do gry
        // Dodaj ilosci odpowiednich kart
        // Szufluj

        deckCards = null;
    }

    public Card? DrawCard()
    {
        return null;
    }
    
    
}