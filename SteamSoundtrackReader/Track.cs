namespace SteamSoundtrackReader;

public class Track
{
    public int DiscNumber { get; set; }
    public int TrackNumber { get; set; }
    public string OriginalName { get; set; }
    public TimeSpan? Duration { get; set; }
}