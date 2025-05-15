namespace Hermes.Global.Requests;

public class JoinInfo
{
    public JoinInfoConnection connection { get; set; } = new JoinInfoConnection();
    public Dictionary<string, object> meta { get; set; } = new Dictionary<string, object>();
}