using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using SteamAppInfo.Music;
using SteamAppInfo.Steam.Enums;
using SteamAppInfo.Steam.Models;
using ValveKeyValue;

namespace SteamAppInfo.Extensions;

public static class AppExtensions
{
    /// <summary>
    /// Parse a Steam app into a soundtrack.
    /// </summary>
    /// <param name="app">The app to parse.</param>
    /// <exception cref="ArgumentException">Thrown if the app is not a valid soundtrack.</exception>
    public static Soundtrack ParseSoundtrack(this App app)
    {
        if (app.AppType != AppType.Music)
        {
            throw new ArgumentException("App is not a soundtrack.");
        }

        var originalReleaseDate = app.Data["common"]?["original_release_date"]?.ToUInt32(CultureInfo.CurrentCulture);
        var steamReleaseDate = app.Data["common"]?["steam_release_date"]?.ToUInt32(CultureInfo.CurrentCulture);

        var soundtrack = new Soundtrack()
        {
            Name = app.Name,
            AppId = app.AppId,
            MetaCriticName = app.Data["common"]?["metacritic_name"]?.ToString(CultureInfo.CurrentCulture),
            OriginalReleaseDate = originalReleaseDate.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(originalReleaseDate.Value)
                : null,
            SteamReleaseDate = steamReleaseDate.HasValue
                ? DateTimeOffset.FromUnixTimeSeconds(steamReleaseDate.Value)
                : null,
            ReviewScore = app.Data["common"]?["review_score"]?.ToInt32(CultureInfo.CurrentCulture),
            ReviewPercentage =
                app.Data["common"]?["review_percentage"]?.ToInt32(CultureInfo.CurrentCulture),
            Metadata = new Metadata
            {
                Artist = app.Data["albummetadata"]?["metadata"]?["artist"]?["english"]
                    ?.ToString(CultureInfo.CurrentCulture),
                Composer = app.Data["albummetadata"]?["metadata"]?["composer"]?["english"]
                    ?.ToString(CultureInfo.CurrentCulture),
                Label = app.Data["albummetadata"]?["metadata"]?["Label"]?["english"]
                    ?.ToString(CultureInfo.CurrentCulture),
                Credits = app.Data["albummetadata"]?["metadata"]?["othercredits"]?["english"]
                    ?.ToString(CultureInfo.CurrentCulture),
            },
            InstallDir = app.InstallDir,
            Data = app.Data,
        };

        if (app.Data["albummetadata"]?["tracks"] is IEnumerable<KVObject> tracks)
        {
            foreach (KVObject track in tracks)
            {
                var minutes = track?["m"]?.ToInt32(CultureInfo.CurrentCulture);
                var seconds = track?["s"]?.ToInt32(CultureInfo.CurrentCulture);

                TimeSpan? duration = null;
                if (minutes.HasValue || seconds.HasValue)
                {
                    var totalSeconds = (minutes ?? 0) * 60 + (seconds ?? 0);
                    if (totalSeconds >= 0)
                    {
                        duration = TimeSpan.FromSeconds(totalSeconds);
                    }
                }
                
                var discNumber = track?["discnumber"]?.ToInt32(CultureInfo.CurrentCulture);
                var trackNumber = track?["tracknumber"]?.ToInt32(CultureInfo.CurrentCulture);
                var originalName = track?["originalname"]?.ToString(CultureInfo.CurrentCulture);
                
                if (!discNumber.HasValue || !trackNumber.HasValue || string.IsNullOrWhiteSpace(originalName))
                {
                    continue;
                }
                
                soundtrack.Tracks.Add(new Track()
                {
                    DiscNumber = discNumber.Value,
                    TrackNumber = trackNumber.Value,
                    OriginalName = originalName,
                    Duration = duration
                });
            }
        }

        return soundtrack;
    }

    /// <summary>
    /// Try to parse a Steam app into a soundtrack.
    /// </summary>
    /// <param name="app">The app to parse.</param>
    /// <param name="soundtrack"></param>
    public static bool TryParseSoundtrack(this App app, [NotNullWhen(true)] out Soundtrack? soundtrack)
    {
        try
        {
            soundtrack = app.ParseSoundtrack();
            return true;
        }
        catch
        {
            soundtrack = null;
            return false;
        }
    }
}