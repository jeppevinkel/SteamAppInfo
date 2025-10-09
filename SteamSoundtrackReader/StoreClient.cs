using System.Net.Http.Json;
using System.Text.Json;

namespace SteamSoundtrackReader;

public class StoreClient
{
    private static readonly HttpClient _httpClient = CreateClient();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://store.steampowered.com/api/"),
            Timeout = TimeSpan.FromSeconds(10)
        };
        return client;
    }

    public async Task<Dictionary<int, string>> GetGenreMap(string appId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"appdetails?appids={appId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new Dictionary<int, string>();
            }

            var data = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (data.ValueKind != JsonValueKind.Object)
            {
                return new Dictionary<int, string>();
            }

            if (data.TryGetProperty(appId, out JsonElement app) && app.TryGetProperty("data", out JsonElement appData) &&
                appData.TryGetProperty("genres", out JsonElement genres) && genres.ValueKind == JsonValueKind.Array)
            {
                var map = new Dictionary<int, string>();
                foreach (var x in genres.EnumerateArray())
                {
                    string? idStr = null;
                    string? description = null;
                    try
                    {
                        idStr = x.GetProperty("id").GetString();
                        description = x.GetProperty("description").GetString();
                    }
                    catch
                    {
                        continue;
                    }

                    if (int.TryParse(idStr, out var id))
                    {
                        map[id] = description ?? string.Empty;
                    }
                }
                return map;
            }

            return new Dictionary<int, string>();
        }
        catch
        {
            return new Dictionary<int, string>();
        }
    }
}