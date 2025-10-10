namespace SteamAppInfo.Steam.Models;

/// <summary>
/// Representation of a local library folder.
/// </summary>
public class Library
{
    /// <summary>
    /// Root path of the library.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// HashSet of the AppIDs from all apps in the library.
    /// </summary>
    public HashSet<uint> Apps { get; set; } = [];
}