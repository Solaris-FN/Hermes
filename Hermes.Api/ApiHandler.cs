using System.Text.Json;
using Hermes.Api.Utilities;
using Hermes.Global;
using Newtonsoft.Json;

namespace Hermes.Api;

public class ApiHandler
{
    private static readonly HttpClient _httpClient;
    static ApiHandler()
    {
        string baseApiUrl = HermesGlobal.BaseApiUrl;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseApiUrl)
        };
        
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Hermes-XMPP-Server");
        _httpClient.DefaultRequestHeaders.Add("X-Hermes-Token", HermesGlobal.Token);
        
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public static async Task<T?> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
        
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(jsonContent); 
            }
        
            Logger.Error($"API GET failed: {endpoint}, Status: {response.StatusCode}");
            return default;
        }
        catch (Exception ex)
        {
            Logger.Error($"API GET exception: {endpoint}, Error: {ex.Message}");
            return default;
        }
    }
    
    public static string BuildQueryString(Dictionary<string, string> parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return string.Empty;
            
        var queryParams = parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}");
            
        return "?" + string.Join("&", queryParams);
    }
}