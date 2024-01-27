using System.Data.Common;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;

namespace MemeBE.Models;

public class Card
{
    public int Id { get; set; }
    public string DisplayName { get; set; }
    public string URL { get; set; }
    public List<Effect> EffectList { get; set; }
    public int Target { get; set; }

    public Card(int id, string displayName, string effectList, int target)
    {
        Id = id;
        DisplayName = displayName;
        //EffectList = effectList; pars to List<Effect>
        Target = target;
    }
}

public class Effect
{
    public string EffectName { get; set; }
    public int? Value { get; set; }
}