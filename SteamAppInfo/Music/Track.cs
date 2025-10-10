namespace SteamAppInfo.Music;

/// <summary>
/// An audio track.
/// </summary>
public class Track
{
    /// <summary>
    /// The disc number on the album.
    /// </summary>
    public int DiscNumber { get; set; }

    /// <summary>
    /// The track number within the disc.
    /// </summary>
    public int TrackNumber { get; set; }

    /// <summary>
    /// The original name of the track.
    /// </summary>
    public string OriginalName { get; set; } = string.Empty;

    /// <summary>
    /// The duration of the track.
    /// </summary>
    public TimeSpan? Duration { get; set; }
}