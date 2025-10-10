namespace SteamAppInfo;

public class AlbumData
{
    public List<Track> Tracks { get; set; } = [];
    public Metadata Metadata { get; set; } = new();
}