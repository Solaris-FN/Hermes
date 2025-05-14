using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Hermes.Classes;

public class AuthResponse
{
    [JsonPropertyName("accountId")] 
    public string AccountId { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; }
}
