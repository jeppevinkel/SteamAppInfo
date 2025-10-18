using SteamAppInfo.Extensions;
using SteamAppInfo.Music;
using SteamAppInfo.Steam;
using SteamAppInfo.Steam.Enums;

namespace SteamAppInfo.Example;

class Program
{
    static void Main(string[] args)
    {
        // Create a Steam client with auto-detection of the Steam installation path
        SteamClient steamClient = SteamClient.AutoDetectSteam();
        Console.WriteLine(steamClient.SteamPath);

        // Get all installed apps
        var apps = steamClient.GetApps();
        
        // Filter for all DLCs
        var dlcApps = apps.Where(it => it.AppType == AppType.DLC);
        Console.WriteLine($"Found {dlcApps.Count()} DLCs");

        // Get all soundtracks with parsed metadata
        var soundtracks = steamClient.GetSoundtracks();
        foreach (Soundtrack soundtrack in soundtracks)
        {
            Console.WriteLine(soundtrack.InstallDir is null
                ? $"{soundtrack.Name} ({soundtrack.AppId}): Not currently installed"
                : $"{soundtrack.Name} ({soundtrack.AppId}): {soundtrack.InstallDir}");
        }
        
        // Query raw VDF data
        var firstSoundtrack = soundtracks.FirstOrDefault();

        if (firstSoundtrack is null)
        {
            throw new Exception("No soundtracks found.");
        }
        
        Console.WriteLine($"The artist of {firstSoundtrack.Name} is {firstSoundtrack?.Data["albummetadata"]?["metadata"]?["artist"]?["english"]}");
    }
}