using SteamAppInfo.Music;
using SteamAppInfo.Steam;
using SteamAppInfo.Steam.Enums;
using SteamAppInfo.Steam.Models;

namespace SteamAppInfo.Extensions;

public static class SteamClientExtensions
{
    /// <summary>
    /// Gets all music apps and parses them into soundtrack objects.
    /// </summary>
    /// <param name="steamClient"></param>
    /// <returns></returns>
    public static IEnumerable<Soundtrack> GetSoundtracks(this SteamClient steamClient)
    {
        foreach (App app in steamClient.GetApps())
        {
            if (app.AppType == AppType.Music && app.TryParseSoundtrack(out Soundtrack? soundtrack))
            {
                yield return soundtrack;
            }
        }
    }
}