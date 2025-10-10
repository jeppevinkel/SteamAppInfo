using System.Collections.ObjectModel;
using SteamAppInfo.Steam.Enums;
using ValveKeyValue;

namespace SteamAppInfo.Steam.Models;

/// <summary>
/// Steam app info.
/// </summary>
public class App
{
    /// <summary>
    /// Steam AppID.
    /// </summary>
    public uint AppId { get; set; }
    
    /// <summary>
    /// Name of the app.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Info state of the app.
    /// </summary>
    public InfoState InfoState { get; set; }
    
    /// <summary>
    /// The type of the app.
    /// </summary>
    public AppType AppType { get; set; }

    /// <summary>
    /// Last time the app was updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }

    /// <summary>
    /// Product Info Change Set token.
    /// </summary>
    public ulong Token { get; set; }

    /// <summary>
    /// Hash of text the appinfo VDF.
    /// </summary>
    public required ReadOnlyCollection<byte> Hash { get; set; }
    
    /// <summary>
    /// Hash of the binary VDF data.
    /// </summary>
    public ReadOnlyCollection<byte>? BinaryDataHash { get; set; }

    /// <summary>
    /// Steam change number.
    /// </summary>
    public uint ChangeNumber { get; set; }

    /// <summary>
    /// AppInfo VDF data.
    /// </summary>
    public KVObject Data { get; set; } = new KVObject("unknown", new KVArrayValue());
}