using SteamAppInfo.Extensions;
using SteamAppInfo.Music;
using SteamAppInfo.Steam;
using SteamAppInfo.Steam.Enums;
using SteamAppInfo.Steam.Models;
using ValveKeyValue;

namespace SteamAppInfo.Example;

class Program
{
    static async Task Main(string[] args)
    {
        SteamClient steamClient = SteamClient.AutoDetectSteam();
        Console.WriteLine(steamClient.SteamPath);

        // var apps = steamClient.GetApps();
        //
        // Dictionary<AppType, int> appTypes = [];
        // Dictionary<InfoState, int> infoStates = [];
        //
        // foreach (var app in apps)
        // {
        //     if (!appTypes.TryAdd(app.AppType, 1))
        //     {
        //         appTypes[app.AppType]++;
        //     }
        //     if (!infoStates.TryAdd(app.InfoState, 1))
        //     {
        //         infoStates[app.InfoState]++;
        //     }
        //
        //     if (app.InfoState == InfoState.NoInfo)
        //     {
        //         var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
        //         await using FileStream writer = File.Open(Path.Combine(@".\no-info", $"{app.AppId}.vdf"), FileMode.Create, FileAccess.Write, FileShare.Read);
        //         serializer.Serialize(writer, app.Data);
        //     }
        // }
        //
        // foreach (var appType in appTypes)
        // {
        //     Console.WriteLine($"{appType.Key}: {appType.Value}");
        // }
        //
        // foreach (var infoState in infoStates)
        // {
        //     Console.WriteLine($"{infoState.Key}: {infoState.Value}");
        // }
        //
        // foreach (App app in apps.Where(it => it.AppType == AppType.Beta))
        // {
        //     Console.WriteLine(app.Name);
        // }
        
        var musicApps = steamClient.GetApps().Where(it => it.AppType == AppType.Music);

        foreach (App musicApp in musicApps)
        {
            if (!musicApp.TryParseSoundtrack(out Soundtrack? soundtrack))
            {
                Console.WriteLine($"{musicApp.Name} ({musicApp.AppId}): No soundtrack");
            }
        }

        var soundtracks = steamClient.GetSoundtracks();
        
        foreach (Soundtrack soundtrack in soundtracks)
        {
            Console.WriteLine($"{soundtrack.Name} ({soundtrack.AppId}): {soundtrack.Tracks.Count}");
        }
    }
}