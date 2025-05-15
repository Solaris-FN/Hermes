namespace Hermes.Global.Requests;

public class JoinInfoConnection
{
    public string id { get; set; } = string.Empty;
    public Dictionary<string, object> meta { get; set; } = new Dictionary<string, object>();
    public bool yield_leadership { get; set; } = false;
}