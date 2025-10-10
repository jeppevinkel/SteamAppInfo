using System.Collections.ObjectModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SteamAppInfo.Extensions;
using SteamAppInfo.Steam.Enums;
using ValveKeyValue;

namespace SteamAppInfo.Steam;

/// <summary>
/// Client used to locate and parse Steam files.
/// </summary>
public class SteamClient
{
    private string _steamPath = null!;
    private string _appInfoPath = null!;
    private string _libraryFoldersPath = null!;

    private KVSerializerOptions _kvSerializerOptions = new();

    // appinfo.vdf details
    private uint? _magic = null;
    private uint? _version = null;
    private long? _offset = null;

    /// <summary>
    /// The universe of the Steam installation. (e.g. Public, Beta, Dev)
    /// </summary>
    public Universe? Universe = null;

    /// <summary>
    /// Get or set the path to the Steam install directory.
    /// </summary>
    /// <exception cref="FileNotFoundException">Thrown if appinfo.vdf or libraryfolders.vdf is not found.</exception>
    public string SteamPath
    {
        get => _steamPath;
        private set
        {
            _steamPath = value;
            _appInfoPath = Path.Join(_steamPath, "appcache", "appinfo.vdf");
            _libraryFoldersPath = Path.Join(_steamPath, "steamapps", "libraryfolders.vdf");

            if (!File.Exists(_appInfoPath))
            {
                throw new FileNotFoundException("Could not find Steam appinfo.vdf.", _appInfoPath);
            }

            if (!File.Exists(_appInfoPath) || !File.Exists(_libraryFoldersPath))
            {
                throw new FileNotFoundException("Could not find Steam libraryfolders.vdf.", _libraryFoldersPath);
            }
        }
    }

    /// <summary>
    /// Only use this constructor if you know what you're doing.
    /// </summary>
    /// <param name="steamPath">Path to the Steam install directory.</param>
    public SteamClient(string steamPath)
    {
        SteamPath = steamPath;
    }

    /// <summary>
    /// Auto-detects the Steam installation path.
    /// </summary>
    /// <returns>An instance of <see cref="SteamClient"/> with the path auto-detected.</returns>
    /// <exception cref="Exception">Thrown when the Steam path isn't found.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when the OS isn't recognized.</exception>
    public static SteamClient AutoDetectSteam()
    {
        string? path = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Valve\\Steam") ??
                               RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                                   .OpenSubKey("SOFTWARE\\Valve\\Steam");

            if (key?.GetValue("SteamPath") is string steamPath)
            {
                path = steamPath;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var paths = new[] {".steam", ".steam/steam", ".steam/root", ".local/share/Steam"};

            path = paths
                .Select(it => Path.Join(home, it))
                .FirstOrDefault(steamPath => Directory.Exists(Path.Join(steamPath, "appcache")));
        }
        else if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            path = Path.Join(home, "Steam");
        }
        else
        {
            throw new PlatformNotSupportedException(
                "I don't know how to detect Steam on this platform. Please specify the Steam path manually.");
        }

        if (path is not null && Directory.Exists(path))
        {
            return new SteamClient(path);
        }

        throw new Exception("Steam not found.");
    }

    public void LoadAppInfo()
    {
        using FileStream stream = File.OpenRead(_appInfoPath);
        using var reader = new BinaryReader(stream);

        _magic = reader.ReadUInt32();

        _version = _magic & 0xFF;
        _magic >>= 8;

        if (_magic != 0x07_56_44)
        {
            throw new InvalidDataException($"Unknown magic header: {_magic:X}");
        }

        if (_version is < 39 or > 41)
        {
            throw new InvalidDataException($"Unknown magic version: {_version}");
        }

        Universe = (Universe) reader.ReadUInt32();
        
        if (_version >= 41)
        {
            var stringTableOffset = reader.ReadInt64();
            _offset = reader.BaseStream.Position;
            if (stringTableOffset < 0 || stringTableOffset >= reader.BaseStream.Length)
            {
                throw new InvalidDataException("Invalid string table offset");
            }

            reader.BaseStream.Position = stringTableOffset;
            var stringCount = reader.ReadUInt32();
            var stringPool = new string[stringCount];

            for (var i = 0; i < stringCount; i++)
            {
                stringPool[i] = reader.BaseStream.ReadNullTermUtf8String();
            }

            reader.BaseStream.Position = _offset.Value;

            _kvSerializerOptions.StringTable = new StringTable(stringPool);
        }
    }

    /// <summary>
    /// Get all apps referenced in the appinfo.vdf file.
    /// </summary>
    public IEnumerable<App> GetApps()
    {
        if (_magic is null || _version is null || _offset is null)
        {
            LoadAppInfo();
        }
        
        using FileStream stream = File.OpenRead(_appInfoPath);
        using var reader = new BinaryReader(stream);

        reader.BaseStream.Position = _offset!.Value;

        var deserializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary);

        List<App> apps = [];

        do
        {
            var appid = reader.ReadUInt32();

            if (appid == 0)
            {
                break;
            }

            var size = reader.ReadUInt32(); // size until the end of Data
            var end = reader.BaseStream.Position + size;

            var app = new App
            {
                AppId = appid,
                InfoState = (InfoState)reader.ReadUInt32(),
                LastUpdated = DateTimeOffset.FromUnixTimeSeconds(reader.ReadUInt32()),
                Token = reader.ReadUInt64(),
                Hash = new ReadOnlyCollection<byte>(reader.ReadBytes(20)),
                ChangeNumber = reader.ReadUInt32(),
            };

            if (_version >= 40)
            {
                app.BinaryDataHash = new ReadOnlyCollection<byte>(reader.ReadBytes(20));
            }
            
            app.Data = deserializer.Deserialize(stream, _kvSerializerOptions);
            

            if (app.Data["common"] is not null)
            {
                app.Name = app.Data["common"]["name"].ToString(CultureInfo.InvariantCulture);
                if (Enum.TryParse(typeof(AppType), app.Data["common"]["type"].ToString(CultureInfo.InvariantCulture), true, out var appType))
                {
                    app.AppType = (AppType) appType;
                }
                else
                {
                    app.AppType = AppType.Unknown;
                }
                
                apps.Add(app);
            }

            if (reader.BaseStream.Position == end) continue;
            // Skip to the end of this entry if sizes don't match
            reader.BaseStream.Position = end;
        } while (true);
        
        return apps;
    }
}