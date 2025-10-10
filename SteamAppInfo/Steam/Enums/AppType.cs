namespace SteamAppInfo.Steam.Enums;

/// <summary>
/// The type of the app.
/// </summary>
public enum AppType
{
    /// <summary>
    /// The type is unknown/outside the scope of this library.
    /// </summary>
    Unknown,
    Config,
    /// <summary>
    /// The app is a game.
    /// </summary>
    Game,
    /// <summary>
    /// The app is a tool.
    /// </summary>
    Tool,
    /// <summary>
    /// The app is a demo.
    /// </summary>
    Demo,
    /// <summary>
    /// The app is a DLC.
    /// </summary>
    DLC,
    Application,
    /// <summary>
    /// The app is a soundtrack.
    /// </summary>
    Music,
    Beta,
    Video,
    Hardware,
}