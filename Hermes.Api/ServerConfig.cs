namespace Hermes.Api;

public class ServerConfig
{
    public string ServerName { get; set; } = "HermesServer";
    public string Environment { get; set; } = "Development";
    public int HttpPort { get; set; } = 8080;
}