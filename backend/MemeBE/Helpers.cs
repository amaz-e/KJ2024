using MemeBE.Models;

namespace MemeBE;

public class Helpers
{
    public static List<Card> Cards { get; set; }
    
    public static void ParseCardsFromCSV()
    {
        
        string filePath = @"./cards.csv";
        var cardList = new List<Card>();
        
        // Check if the file exists
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        // Read all lines from the file
        var lines = File.ReadAllLines(filePath);

        // Skip the header row and start processing from the first data row
        for (int i = 1; i < lines.Length; i++)
        {
            var columns = lines[i].Split(',');

            // Assuming the CSV format is: FirstName,LastName,Age
            int.TryParse(columns[0], out int id);
            var displayName = columns[1];
            var effectList = columns[2];
            int.TryParse(columns[3], out int target);


            var person = new Card(id, displayName, effectList, target); 
            cardList.Add(person);
        }

        Cards = cardList;
    }
    
}