using System.Buffers;
using System.Text;

namespace SteamAppInfo.Extensions;

/// <summary>
/// Various extension methods for working with streams.
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// Reads a stream until a null terminator is found.
    /// </summary>
    /// <param name="stream">Stream to read.</param>
    /// <returns>A UTF-8 decoded string from the bytes.</returns>
    public static string ReadNullTermUtf8String(this Stream stream)
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