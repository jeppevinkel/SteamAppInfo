using ValveKeyValue;

namespace SteamAppInfo.Music;

/// <summary>
/// An object representation of a Steam soundtrack.
/// </summary>
public class Soundtrack
{
    /// <summary>
    /// The Steam AppID associated with the soundtrack.
    /// </summary>
    public required uint AppId { get; set; }

    /// <summary>
    /// The name of the soundtrack.
    /// </summary>
    public required string Name { get; set; } = string.Empty;

    /// <summary>
    /// The meta-critic name of the soundtrack (if available).
    /// </summary>
    public string? MetaCriticName { get; set; }
    
    /// <summary>
    /// The original release date of the soundtrack (if available).
    /// </summary>
    public DateTimeOffset? OriginalReleaseDate { get; set; }
    
    /// <summary>
    /// The Steam release date of the soundtrack (if available).
    /// </summary>
    public DateTimeOffset? SteamReleaseDate { get; set; }
    
    /// <summary>
    /// The review score of the soundtrack (if available).
    /// </summary>
    public int? ReviewScore { get; set; }
    
    /// <summary>
    /// The review percentage of the soundtrack (if available).
    /// </summary>
    public int? ReviewPercentage { get; set; }
    
    /// <summary>
    /// The installation directory of the soundtrack.
    /// </summary>
    public string? InstallDir { get; set; }
    
    /// <summary>
    /// Soundtrack metadata.
    /// </summary>
    public Metadata Metadata { get; set; } = new();
    
    /// <summary>
    /// A list of all tracks on the soundtrack.
    /// </summary>
    public List<Track> Tracks { get; set; } = [];
    
    /// <summary>
    /// The primary genre of the soundtrack (if available).
    /// </summary>
    public string PrimaryGenre { get; set; } = string.Empty;
    
    /// <summary>
    /// A list of all genres of the soundtrack (if available).
    /// </summary>
    public List<string> Genres { get; set; } = [];

    /// <summary>
    /// AppInfo VDF data.
    /// </summary>
    public KVObject Data { get; set; } = new KVObject("unknown", new KVArrayValue());
}