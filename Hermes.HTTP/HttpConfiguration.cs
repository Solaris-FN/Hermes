namespace Hermes.HTTP;

public class HttpConfiguration
{
    public int Port { get; set; } = 8080;
    public List<string> Prefixes { get; set; } = new List<string>();
    
    public HttpConfiguration()
    {
        Prefixes.Add($"http://localhost:{Port}/");
    }

    public HttpConfiguration(int port)
    {
        Port = port;
        Prefixes.Add($"http://localhost:{port}/");
    }

    public void AddPrefix(string prefix)
    {
        if (!prefix.EndsWith("/"))
            prefix += "/";
                
        Prefixes.Add(prefix);
    }
}