namespace SteamAppInfo;

public class Track
{
    public int DiscNumber { get; set; }
    public int TrackNumber { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public TimeSpan? Duration { get; set; }
}