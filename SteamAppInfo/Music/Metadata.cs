namespace SteamAppInfo.Music;

/// <summary>
/// Soundtrack metadata.
/// </summary>
public class Metadata
{
    /// <summary>
    /// The artist of the soundtrack.
    /// </summary>
    public string? Artist { get; set; }

    /// <summary>
    /// The composer of the soundtrack.
    /// </summary>
    public string? Composer { get; set; }

    /// <summary>
    /// The label behind the soundtrack.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Extra credits for the soundtrack.
    /// </summary>
    public string? Credits { get; set; }
}