namespace SteamSoundtrackReader;

public class Soundtrack
{
    public string AppId { get; set; }
    public string Name { get; set; }
    public string? MetaCriticName { get; set; }
    public DateTime? OriginalReleaseDate { get; set; }
    public DateTime? SteamReleaseDate { get; set; }
    public int? ReviewScore { get; set; }
    public int? ReviewPercentage { get; set; }
    public string InstallDir { get; set; }
    public AlbumData AlbumData { get; set; }
    public string PrimaryGenre { get; set; }
    public List<string> Genres { get; set; } = [];
}