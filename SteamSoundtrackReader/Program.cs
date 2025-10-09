using System.Buffers;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ValveKeyValue;

namespace SteamSoundtrackReader;

class Program
{
    static async Task Main(string[] args)
    {
        if (!Directory.Exists(@".\apps"))
        {
            Directory.CreateDirectory(@".\apps");
        }

        if (!Directory.Exists(@".\albums"))
        {
            Directory.CreateDirectory(@".\albums");
        }

        await using var stream = new FileStream(@"C:\Program Files (x86)\Steam\appcache\appinfo.vdf", FileMode.Open,
            FileAccess.Read, FileShare.ReadWrite);

        GetVdfType(stream);

        await Read(stream);
    }

    private static void GetVdfType(FileStream stream)
    {
        stream.Position = 1;
        var typeByte = stream.ReadByte();
        stream.Position = 0;

        Console.Write($"{stream.Name}: ");

        switch (typeByte)
        {
            case 0x44:
                Console.WriteLine("AppInfo");
                break;
            case 0x55:
                Console.WriteLine("PackageInfo");
                break;
            default:
                Console.WriteLine("Unknown");
                break;
        }
    }

    public static async Task Read(Stream input)
    {
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
                AppID = appid,
                InfoState = reader.ReadUInt32(),
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
                throw new InvalidDataException();
            }

            if (app.Data["common"]?["type"]?.ToString() == "Music")
            {
                var appName = app.Data["common"]?["name"]?.ToString(CultureInfo.CurrentCulture) ??
                              throw new Exception("Missing name");
                var appId = app.Data["appid"]?.ToString(CultureInfo.CurrentCulture) ??
                            throw new Exception("Missing appid");
                Console.WriteLine($"{appName} ({appId})");
                var soundtrack = new Soundtrack()
                {
                    Name = app.Data["common"]?["name"]?.ToString(CultureInfo.CurrentCulture) ??
                           throw new Exception("Missing name"),
                    AppId = app.Data["appid"]?.ToString(CultureInfo.CurrentCulture) ??
                            throw new Exception("Missing appid"),
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
                soundtrack.InstallDir = app.Data["config"]?["installdir"]?.ToString(CultureInfo.CurrentCulture) ??
                                        throw new Exception("Missing installdir");

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

                StoreClient storeClient = new StoreClient();
                var genreMap = await storeClient.GetGenreMap(appId);

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

                foreach (var track in app.Data["albummetadata"]["tracks"] as IEnumerable<KVObject>)
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

                    soundtrack.AlbumData.Tracks.Add(new Track()
                    {
                        DiscNumber = track?["discnumber"]?.ToInt32(CultureInfo.CurrentCulture) ??
                                     throw new Exception("Missing track discnumber"),
                        TrackNumber = track?["tracknumber"]?.ToInt32(CultureInfo.CurrentCulture) ??
                                      throw new Exception("Missing track tracknumber"),
                        OriginalName = track?["originalname"]?.ToString(CultureInfo.CurrentCulture) ??
                                       throw new Exception("Missing track originalname"),
                        Duration = duration
                    });
                }

                var json = JsonSerializer.Serialize(soundtrack, new JsonSerializerOptions()
                {
                    WriteIndented = true,
                });

                File.WriteAllText(Path.Combine(@".\albums", $"{appid}.json"), json);

                var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
                using FileStream writer = File.OpenWrite(Path.Combine(@".\albums", $"{appid}.vdf"));

                serializer.Serialize(writer, app.Data);
            }
        } while (true);
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

                buffer[position++] = (byte) b;
            } while (true);

            return Encoding.UTF8.GetString(buffer[..position]);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}