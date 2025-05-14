using Fleck;
using Newtonsoft.Json;

namespace Hermes.Classes;

public class SocketClientDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [JsonIgnore]
    public IWebSocketConnection Socket { get; set; }
    public bool IsLoggedIn { get; set; } = false;
    public bool IsAuthenticated { get; set; } = false;
    public string AccountId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Jid { get; set; } = string.Empty;
    public string Resource { get; set; } = string.Empty;
    public LastPresenceUpdate LastPresenceUpdate { get; set; } = new();
}