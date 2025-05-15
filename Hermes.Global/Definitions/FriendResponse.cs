using Newtonsoft.Json;

namespace Hermes.Global.Definitions;

public class FriendResponse
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;
    [JsonProperty("direction")]
    public string Direction { get; set; } = string.Empty;
}