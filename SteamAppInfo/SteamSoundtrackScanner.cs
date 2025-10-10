using System.Buffers;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using SteamAppInfo.Steam.Enums;
using SteamAppInfo.Steam.Models;
using ValveKeyValue;

namespace SteamAppInfo;

/// <summary>
/// High-level API for reading Steam appinfo.vdf and extracting soundtrack metadata.
/// </summary>
public class SteamSoundtrackScanner
{
    /// <summary>
    /// Parse Steam's appinfo.vdf and return a list of soundtrack entries.
    /// </summary>
    /// <param name="appInfoPath">Full path to appcache\\appinfo.vdf</param>
    /// <param name="libraryFoldersVdfPath">Full path to steamapps\\libraryfolders.vdf used to resolve install directories.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of discovered soundtracks with metadata.</returns>
    public async Task<List<Soundtrack>> ScanAsync(string appInfoPath, string libraryFoldersVdfPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appInfoPath)) throw new ArgumentException("Value cannot be null or empty.", nameof(appInfoPath));
        if (string.IsNullOrWhiteSpace(libraryFoldersVdfPath)) throw new ArgumentException("Value cannot be null or empty.", nameof(libraryFoldersVdfPath));
        if (!File.Exists(appInfoPath)) throw new FileNotFoundException("Could not find appinfo.vdf", appInfoPath);
        if (!File.Exists(libraryFoldersVdfPath)) throw new FileNotFoundException("Could not find libraryfolders.vdf", libraryFoldersVdfPath);

        var libraryFolders = LibraryFolders.Read(libraryFoldersVdfPath);
        var storeClient = new StoreClient();

        await using var stream = new FileStream(appInfoPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        return await ReadInternal(stream, storeClient, libraryFolders, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<Soundtrack>> ReadInternal(Stream input, StoreClient storeClient, LibraryFolders libraryFolders, CancellationToken cancellationToken)
    {
        var soundtracks = new List<Soundtrack>();

        using var reader = new BinaryReader(input);
        var magic = reader.ReadUInt32();

        var version = magic & 0xFF;
        magic >>= 8;

        if (magic != 0x07_56_44)
        {
            throw new InvalidDataException($"Unknown magic header: {magic:X}");
        }

        if (version < 39 || version > 41)
        {
            throw new InvalidDataException($"Unknown magic version: {version}");
        }

        _ = reader.ReadUInt32();

        var options = new KVSerializerOptions();

        if (version >= 41)
        {
            var stringTableOffset = reader.ReadInt64();
            var offset = reader.BaseStream.Position;
            if (stringTableOffset < 0 || stringTableOffset >= reader.BaseStream.Length)
            {
                throw new InvalidDataException("Invalid string table offset");
            }
            reader.BaseStream.Position = stringTableOffset;
            var stringCount = reader.ReadUInt32();
            var stringPool = new string[stringCount];

            for (var i = 0; i < stringCount; i++)
            {
                stringPool[i] = ReadNullTermUtf8String(reader.BaseStream);
            }

            reader.BaseStream.Position = offset;

            options.StringTable = new(stringPool);
        }

        var deserializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary);

        do
        {
            var appid = reader.ReadUInt32();

            if (appid == 0)
            {
                break;
            }

            var size = reader.ReadUInt32(); // size until end of Data
            var end = reader.BaseStream.Position + size;

            var app = new App
            {
                AppId = appid,
                InfoState = (InfoState)reader.ReadUInt32(),
                LastUpdated = DateTimeFromUnixTime(reader.ReadUInt32()),
                Token = reader.ReadUInt64(),
                Hash = new ReadOnlyCollection<byte>(reader.ReadBytes(20)),
                ChangeNumber = reader.ReadUInt32(),
            };

            if (version >= 40)
            {
                app.BinaryDataHash = new ReadOnlyCollection<byte>(reader.ReadBytes(20));
            }

            app.Data = deserializer.Deserialize(input, options);

            if (reader.BaseStream.Position != end)
            {
                // Skip to the end of this entry if sizes don't match
                reader.BaseStream.Position = end;
                continue;
            }

            if (app.Data["common"]?["type"]?.ToString() == "Music")
            {
                var appName = app.Data["common"]?["name"]?.ToString(CultureInfo.CurrentCulture);
                var appId = app.Data["appid"]?.ToString(CultureInfo.CurrentCulture);
                if (string.IsNullOrWhiteSpace(appName) || string.IsNullOrWhiteSpace(appId))
                {
                    reader.BaseStream.Position = end;
                    continue;
                }

                var soundtrack = new Soundtrack()
                {
                    Name = appName,
                    AppId = appId,
                    MetaCriticName = app.Data["common"]?["metacritic_name"]?.ToString(CultureInfo.CurrentCulture),
                };

                var originalReleaseDate =
                    app.Data["common"]?["original_release_date"]?.ToUInt32(CultureInfo.CurrentCulture);
                var steamReleaseDate = app.Data["common"]?["steam_release_date"]?.ToUInt32(CultureInfo.CurrentCulture);

                soundtrack.OriginalReleaseDate = originalReleaseDate.HasValue
                    ? DateTimeFromUnixTime(originalReleaseDate.Value)
                    : null;
                soundtrack.SteamReleaseDate =
                    steamReleaseDate.HasValue ? DateTimeFromUnixTime(steamReleaseDate.Value) : null;
                soundtrack.ReviewScore = app.Data["common"]?["review_score"]?.ToInt32(CultureInfo.CurrentCulture);
                soundtrack.ReviewPercentage =
                    app.Data["common"]?["review_percentage"]?.ToInt32(CultureInfo.CurrentCulture);

                string installDir = string.Empty;
                var library = libraryFolders.GetLibraryFromAppId(appId);
                if (app.Data["config"]?["installdir"]?.ToString(CultureInfo.CurrentCulture) is not null && library is not null)
                {
                    installDir = Path.Combine(library.Path, app.Data["config"]["installdir"].ToString(CultureInfo.CurrentCulture));
                }

                soundtrack.InstallDir = installDir;

                soundtrack.AlbumData = new AlbumData()
                {
                    Metadata = new Metadata()
                    {
                        Artist = app.Data["albummetadata"]?["metadata"]?["artist"]?["english"]
                            ?.ToString(CultureInfo.CurrentCulture),
                        Composer = app.Data["albummetadata"]?["metadata"]?["composer"]?["english"]
                            ?.ToString(CultureInfo.CurrentCulture),
                        Label = app.Data["albummetadata"]?["metadata"]?["Label"]?["english"]
                            ?.ToString(CultureInfo.CurrentCulture),
                        Credits = app.Data["albummetadata"]?["metadata"]?["othercredits"]?["english"]
                            ?.ToString(CultureInfo.CurrentCulture),
                    }
                };

                var genreMap = await storeClient.GetGenreMap(appId, cancellationToken).ConfigureAwait(false);

                if (app.Data["common"]?["primary_genre"] is not null)
                {
                    if (genreMap.TryGetValue(app.Data["common"]["primary_genre"].ToInt32(CultureInfo.CurrentCulture),
                            out var genreName))
                    {
                        soundtrack.PrimaryGenre = genreName;
                    }
                }

                if (app.Data["common"]?["genres"] is not null &&
                    app.Data["common"]?["genres"] is IEnumerable<KVObject> genres)
                {
                    foreach (KVObject genre in genres)
                    {
                        if (genreMap.TryGetValue(genre.Value.ToInt32(CultureInfo.CurrentCulture), out var genreName))
                        {
                            soundtrack.Genres.Add(genreName);
                        }
                    }
                }

                if (app.Data["albummetadata"]?["tracks"] is IEnumerable<KVObject> trackList)
                {
                    foreach (var track in trackList)
                    {
                        var minutes = track?["m"]?.ToInt32(CultureInfo.CurrentCulture);
                        var seconds = track?["s"]?.ToInt32(CultureInfo.CurrentCulture);

                        TimeSpan? duration = null;
                        if (minutes.HasValue || seconds.HasValue)
                        {
                            var totalSeconds = (minutes ?? 0) * 60 + (seconds ?? 0);
                            if (totalSeconds >= 0)
                            {
                                duration = TimeSpan.FromSeconds(totalSeconds);
                            }
                        }

                        var discNumber = track?["discnumber"]?.ToInt32(CultureInfo.CurrentCulture);
                        var trackNumber = track?["tracknumber"]?.ToInt32(CultureInfo.CurrentCulture);
                        var originalName = track?["originalname"]?.ToString(CultureInfo.CurrentCulture);

                        if (!discNumber.HasValue || !trackNumber.HasValue || string.IsNullOrWhiteSpace(originalName))
                        {
                            continue;
                        }

                        soundtrack.AlbumData.Tracks.Add(new Track()
                        {
                            DiscNumber = discNumber.Value,
                            TrackNumber = trackNumber.Value,
                            OriginalName = originalName,
                            Duration = duration
                        });
                    }
                }

                soundtracks.Add(soundtrack);
            }
        } while (true);

        return soundtracks;
    }

    private static DateTime DateTimeFromUnixTime(uint unixTime)
    {
        return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTime);
    }

    private static string ReadNullTermUtf8String(Stream stream)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(32);

        try
        {
            var position = 0;

            do
            {
                var b = stream.ReadByte();

                if (b <= 0) // null byte or stream ended
                {
                    break;
                }

                if (position >= buffer.Length)
                {
                    var newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = newBuffer;
                }

                buffer[position++] = (byte)b;
            } while (true);

            return Encoding.UTF8.GetString(buffer[..position]);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}