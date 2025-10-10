# Getting Started

Using this library is meant to be as simple as possible.  
To get started, first instantiate the SteamClient class.

```csharp
// Helper function that automatically finds where Steam is installed. Works on Windows, Linux and Mac with most standard installs.
SteamClient steamClient = SteamClient.AutoDetectSteam();

// Or
SteamClient steamClient = new SteamClient(@"C:\Program Files (x86)\Steam");
```

With the SteamClient instance, you can get all Steam apps from the local app cache.

```csharp
IEnumerable<App> apps = steamClient.GetApps();
```