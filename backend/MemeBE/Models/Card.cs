using System.Data.Common;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;

namespace MemeBE.Models;

public class Card
{
    public int Id { get; set; }
    public int? DeckId { get; set; }
    public string DisplayName { get; set; }
    public string URL { get; set; }
    public List<Effect> EffectList { get; set; }
    public int Target { get; set; }

    public Card(int id, string displayName, string url, string effectList, int target)
    {
        Id = id;
        DisplayName = displayName;
        URL = url;
        EffectList = ParseEffects(effectList);
        Target = target;
    }

    private List<Effect> ParseEffects(string effectList)
    {
        List<Effect> resultList = new List<Effect>();
        var parseByPipe = effectList.Split("|");
        foreach (var singleEffect in parseByPipe)
        {
            Effect e = new Effect();
            e.EffectName = singleEffect.Split(",")[0];
            e.Value = int.Parse(singleEffect.Split(",")[1]);
            resultList.Add(e);
        }

        return resultList;
    }
}

public class Effect
{
    public string EffectName { get; set; }
    public int? Value { get; set; }
}
