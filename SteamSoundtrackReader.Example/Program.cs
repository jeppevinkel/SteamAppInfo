using SteamSoundtrackReader;

namespace SteamSoundtrackReader.Example;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Determine default paths
            string appInfoPath = args.Length > 0 ? args[0] : new[]
            {
                @"C:\\Program Files (x86)\\Steam\\appcache\\appinfo.vdf",
                @"C:\\Program Files\\Steam\\appcache\\appinfo.vdf"
            }.FirstOrDefault(File.Exists) ?? string.Empty;

            string libraryFoldersPath = args.Length > 1 ? args[1] : new[]
            {
                @"C:\\Program Files (x86)\\Steam\\steamapps\\libraryfolders.vdf",
                @"C:\\Program Files\\Steam\\steamapps\\libraryfolders.vdf"
            }.FirstOrDefault(File.Exists) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(appInfoPath) || string.IsNullOrWhiteSpace(libraryFoldersPath))
            {
                Console.Error.WriteLine("Could not find Steam appinfo.vdf or libraryfolders.vdf. Pass their full paths as arguments.");
                return;
            }

            var scanner = new SteamSoundtrackScanner();
            var results = await scanner.ScanAsync(appInfoPath, libraryFoldersPath);

            Console.WriteLine($"Found {results.Count} soundtrack(s).");
            foreach (var s in results.Take(10))
            {
                Console.WriteLine($"- {s.Name} ({s.AppId}) | Primary genre: {s.PrimaryGenre}");
            }

            if (results.Count > 10)
            {
                Console.WriteLine($"...and {results.Count - 10} more");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
        }
    }
}