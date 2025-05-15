namespace Hermes.Global.Requests;

public class PartyCreate
{
    public Dictionary<string, object> config { get; set; } = new Dictionary<string, object>();
    public JoinInfo join_info { get; set; } = new JoinInfo();
    public Dictionary<string, object> meta { get; set; } = new Dictionary<string, object>();
}