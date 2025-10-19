# SteamAppInfo
[![NuGet Version](https://img.shields.io/nuget/v/SteamAppInfo)](https://www.nuget.org/packages/SteamAppInfo/)

This is a project that aims to provide a simple interface for retrieving Steam app information from the appinfo.vdf file within the local appcache.

## Usage

For a more comprehensive example, see the [SteamAppInfo.Example](https://github.com/jeppevinkel/SteamAppInfo/blob/main/SteamAppInfo.Example/Program.cs) project.

### Get a list of all Steam apps

```csharp
// Helper function that automatically finds where Steam is installed. Works on Windows, Linux and Mac with most standard installs.
SteamClient steamClient = SteamClient.AutoDetectSteam();

var apps = steamClient.GetApps();
```

### Structure of apps

Apps have a simple structure:

```csharp
public class App
{
    public string Name { get; set; }
    public uint AppId { get; set; }
    public InfoState InfoState { get; set; }
    public AppType AppType { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
    public ulong Token { get; set; }
    public ReadOnlyCollection<byte> Hash { get; set; }
    public ReadOnlyCollection<byte>? BinaryDataHash { get; set; }
    public uint ChangeNumber { get; set; }
    public KVObject Data { get; set; }
}
```

```csharp
public enum InfoState
{
    NoInfo = 1,
    Normal = 2
}
```

```csharp
public enum AppType
{
    Unknown,
    Config,
    Game,
    Tool,
    Demo,
    DLC,
    Application,
    Music,
    config,
    Beta,
    Video,
    Hardware
}
```

For documentation on the KVObject, see [here](https://github.com/ValveResourceFormat/ValveKeyValue).

## Special mentions

This project would not have been possible without the [ValveKeyValue](https://github.com/ValveResourceFormat/ValveKeyValue) project by xPaw.
