using System.Data.Common;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic;

namespace MemeBE.Models;

public class Card
{
    public int Id { get; set; }
    public string DisplayName { get; set; }

    public Card(int id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }
}