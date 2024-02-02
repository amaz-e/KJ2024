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
        for (int i = 0; i < lines.Length; i++)
        {
            try {
            var columns = lines[i].Split(';');

            // Assuming the CSV format is: FirstName,LastName,Age
            int.TryParse(columns[0], out int id);
            var displayName = columns[1];
            var url = columns[2];
            var effectList = columns[3];
            int.TryParse(columns[4], out int target);


            var person = new Card(id, displayName, url, effectList, target); 
            cardList.Add(person);
            }catch{
            Console.Writeln("Error reading card " + i);
            }
        }

        Cards = cardList;
    }
        public static string GetLaughMessage(string playerNick, int laughValue)
    {
        Random rnd = new Random();
        string[] laughMessages = new string[]
        {
            "Hold onto your funny bones! <b>" + playerNick + "</b> has just boosted the laugh-o-meter with an extra <b>" +
            laughValue + "</b> HAHA points!",
            "Alert! <b>" + playerNick + "</b> is on a chuckle charge, racking up another <b>" + laughValue +
            "</b> HAHA points to their giggle bank!",
            "Whoa, <b>" + playerNick + "</b> is serving up some serious hilarity with an additional <b>" + laughValue +
            "</b> HAHA points! Comedy gold!",
            "What's that sound? It's <b>" + playerNick + "</b> laughing all the way to the HAHA hall of fame with <b>" +
            laughValue + "</b> more points of pure mirth!",
            "Breaking news: <b>" + playerNick + "</b>'s laughter level just skyrocketed by <b>" + laughValue +
            "</b> HAHA points. The fun-o-sphere is off the charts!",
            "Gather 'round, folks! Witness <b>" + playerNick + "</b> sprinkle a dash of chuckles, adding <b>" + laughValue +
            "</b> HAHA points to their giggle tally!",
            "It's no joke! <b>" + playerNick + "</b> is officially a laughter legend, piling on an extra <b>" + laughValue +
            "</b> HAHA points to their chuckle stash!",
            "Behold! <b>" + playerNick + "</b> has unleashed a laughnado, swirling up <b>" + laughValue +
            "</b> HAHA points in a frenzy of funniness!",
            "Can you feel the ground shaking? That's just <b>" + playerNick + "</b> laughing up an earthquake with another <b>" +
            laughValue + "</b> HAHA points on the Richter scale!",
            "Quick, get the giggle goggles! <b>" + playerNick + "</b> is blinding us with brilliance, bagging <b>" + laughValue +
            "</b> more HAHA points in a spectacular show of snickers!"
        };

        int randomIndex = rnd.Next(laughMessages.Length);
        return laughMessages[randomIndex];
    }
    public static string GetGrumpyMessage(string playerNick, int grumpyValue)
    {
        Random rnd = new Random();
        string[] grumpyMessages = new string[]
        {
            "Uh oh! <b>" + playerNick + "</b> just unleashed a grump-storm, draining <b>" + grumpyValue + "</b> joy points from the room. Cheer up, buttercup!",
            "Alert the mood squad! <b>" + playerNick + "</b> is having a no-smiles spell, dropping <b>" + grumpyValue + "</b> happiness points like hot potatoes!",
            "Gloomy alert! <b>" + playerNick + "</b> has just cast a little cloud of frowns, sapping <b>" + grumpyValue + "</b> cheer points from the happiness meter!",
            "Who turned down the joy tunes? <b>" + playerNick + "</b> has, with a grumpyValue-sized dip in the vibe vibes!",
            "Hold the chuckles, <b>" + playerNick + "</b> is on a serious spree, swiping <b>" + grumpyValue + "</b> giggle points from the laugh ledger!",
            "Warning! <b>" + playerNick + "</b>'s smile seems to be on a brief break, taking <b>" + grumpyValue + "</b> glee points with it!",
            "Looks like <b>" + playerNick + "</b> hit a pothole on the joyride, causing a loss of <b>" + grumpyValue + "</b> happy points. Time for a pick-me-up!",
            "Is it just me, or did <b>" + playerNick + "</b> just start a frown-a-thon? There go <b>" + grumpyValue + "</b> points from the joy jar!",
            "Notice: <b>" + playerNick + "</b>’s happiness meter just experienced a slight dip, with a <b>" + grumpyValue + "</b> drop in delight digits!",
            "Eek! <b>" + playerNick + "</b> just tripped on a grumble bump, shedding <b>" + grumpyValue + "</b> bliss bits off their happiness score!"
        };

        int randomIndex = rnd.Next(grumpyMessages.Length);
        return grumpyMessages[randomIndex];
    }
    
    public static string GetPersistantMessage(string playerNick, string effectName)
    {
        Random rnd = new Random();
        string[] persistantMessage = new string[]{playerNick + " Did something completely different"};
        if (effectName.Equals("Buff"))
        {
            persistantMessage = new string[]
            {
                "Watch out! <b>" + playerNick +
                "</b> just played a buff card, bolstering their strength for the next round.",
                "Keep your eyes peeled! <b>" + playerNick +
                "</b> has just set up a powerful buff, enhancing their capabilities.",
                "Brace yourselves, <b>" + playerNick + "</b> boosts their arsenal with a well-timed buff card.",
                "Calculating... <b>" + playerNick +
                "</b> plays a buff card, and their power grows silently but surely.",
                "Anticipation builds as <b>" + playerNick +
                "</b> activates a buff card, their strength quietly surging.",
                "Subtle yet effective, <b>" + playerNick + "</b> places a buff card, setting the stage for a comeback.",
                "Poised and ready, <b>" + playerNick + "</b> empowers themselves with a game-changing buff card.",
                "Strength in stillness, <b>" + playerNick + "</b> has played a buff card, their power lying in wait.",
                "Quiet before the storm, <b>" + playerNick +
                "</b> lays down a buff card, their potential energy growing.",
                "Ready for the future, <b>" + playerNick + "</b> buffs up, laying the groundwork for victory."
            };
        }
        else if(effectName.Equals("Shield"))

        {
            persistantMessage = new string[]
            {
                "Strategic mastery! <b>" + playerNick + "</b> fortifies their defenses with an impenetrable shield card.",
                "Shrewd move by <b>" + playerNick + "</b> as they deploy a shield card, preparing for whatever comes next!",
                "With a flick of the wrist, <b>" + playerNick + "</b> lays down a shield card, warding off future attacks.",
                "The tides may turn! <b>" + playerNick + "</b> casts a shield, raising their defenses to new heights.",
                "A moment of peace as <b>" + playerNick + "</b> reinforces their position with a sturdy shield card.",
                "Defensive stance! <b>" + playerNick + "</b> has played a shield card, securing their ground.",
                "An aura of protection envelops <b>" + playerNick + "</b> as they play a strategic shield card.",
                "<b>" + playerNick + "</b> casts a shield, cloaking themselves in an invisible but unbreakable barrier.",
                "A shield card enters the field! <b>" + playerNick + "</b> is now shielded against the next onslaught.",
                "Defense is the best offense! <b>" + playerNick + "</b> places a shield card with a knowing smile."

            };
        }

        int randomIndex = rnd.Next(persistantMessage.Length);
        return persistantMessage[randomIndex];
    }
    
}
