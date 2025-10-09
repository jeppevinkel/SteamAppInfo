using System.Net.Http.Json;
using System.Text.Json;

namespace SteamSoundtrackReader;

public class StoreClient
{
    private readonly HttpClient _httpClient;
    
    public StoreClient()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri("https://store.steampowered.com/api/");
    }
    
    public async Task<Dictionary<int, string>> GetGenreMap(string appId)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"appdetails?appids={appId}");
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();

        if (data.TryGetProperty(appId, out JsonElement app) && app.TryGetProperty("data", out JsonElement appData) &&
            appData.TryGetProperty("genres", out JsonElement genres))
        {
            return genres.EnumerateArray()
                .ToDictionary(x => int.Parse(x.GetProperty("id").GetString()), x => x.GetProperty("description").GetString());
        }

        return [];
    }
}