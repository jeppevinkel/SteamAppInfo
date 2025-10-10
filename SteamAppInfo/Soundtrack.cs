namespace SteamAppInfo;

public class Soundtrack
{
    public string AppId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? MetaCriticName { get; set; }
    public DateTime? OriginalReleaseDate { get; set; }
    public DateTime? SteamReleaseDate { get; set; }
    public int? ReviewScore { get; set; }
    public int? ReviewPercentage { get; set; }
    public string InstallDir { get; set; } = string.Empty;
    public AlbumData AlbumData { get; set; } = new();
    public string PrimaryGenre { get; set; } = string.Empty;
    public List<string> Genres { get; set; } = [];
}