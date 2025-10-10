namespace SteamAppInfo.Steam.Enums;

/// <summary>
/// The universe of the Steam client.
/// </summary>
public enum Universe
{
    /// <summary>
    /// Indicates the client is running in an invalid/unknown branch.
    /// </summary>
    Invalid = 0,
    /// <summary>
    /// Indicates the client is running in the public branch.
    /// </summary>
    Public = 1,
    /// <summary>
    /// Indicates the client is running in the beta branch.
    /// </summary>
    Beta = 2,
    /// <summary>
    /// Indicates the client is running in the internal branch.
    /// </summary>
    Internal = 3,
    /// <summary>
    /// Indicates the client is running in the development branch.
    /// </summary>
    Dev = 4,
    /// <summary>
    /// Indicates the client is running in the maximum branch.
    /// </summary>
    Max = 5,
}