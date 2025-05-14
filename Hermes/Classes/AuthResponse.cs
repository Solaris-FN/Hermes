using Newtonsoft.Json;

namespace Hermes.Classes;

public class AuthResponse
{
    [JsonProperty("accountId")]
    public string AccountId { get; set; } = string.Empty;
    [JsonProperty("username")]
    public string Username { get; set; } = string.Empty;
}