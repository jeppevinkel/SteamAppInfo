using System.Globalization;
using ValveKeyValue;

namespace SteamSoundtrackReader;

public class LibraryFolders
{
    public List<Library> Libraries { get; set; } = [];

    public Library? GetLibraryFromAppId(string appId)
    {
        return Libraries.FirstOrDefault(library => library.Apps.Contains(appId));
    }
    
    public static LibraryFolders Read(string vdfPath)
    {
        using FileStream stream = File.OpenRead(vdfPath);

        var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
        var data = serializer.Deserialize(stream);
        
        var libraryFolders = new LibraryFolders();

        if (data is IEnumerable<KVObject> libraries)
        {
            foreach (KVObject library in libraries)
            {
                var path = library["path"]?.ToString(CultureInfo.CurrentCulture);
                if (path is null || library["apps"] is not IEnumerable<KVObject> apps)
                {
                    continue;
                }
                
                libraryFolders.Libraries.Add(new Library()
                {
                    Path = path.Replace(@"\\", @"\"),
                    Apps = apps.Select(it => (it.Name.ToString(CultureInfo.CurrentCulture))).ToHashSet()
                });
            }
        }

        return libraryFolders;
    }
    
    public class Library
    {
        public required string Path { get; set; }
        public HashSet<string> Apps { get; set; } = new HashSet<string>();
    }
}