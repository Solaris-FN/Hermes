using System.Collections.Concurrent;
using Fleck;
using Hermes.Global.Definitions;

namespace Hermes.Global;

public static class HermesGlobal
{
    public static ConcurrentDictionary<Guid, SocketClientDefinition> _clients;
    public static List<PartyDefinition> parties { get; set; } = new List<PartyDefinition>();
    
    public static string Domain = "hermes.solarisfn.org";
    public static string BaseApiUrl = "https://api-v1-horizon-external-api.solarisfn.org"; // change this obviously
    public static string Token = "token123verysecurehahahakys";
}