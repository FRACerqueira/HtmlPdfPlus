// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.IO.Compression;
using System.Text.Json;

namespace HtmlPdfPlus.Shared.Core
{
    internal static class GZipHelper
    {
        public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private const int BufferSize = 81920; // 80 KB buffer size

        /// <summary>
        /// Default cap on decompressed output size, used when no explicit limit is supplied.
        /// </summary>
        internal const long DefaultMaxDecompressedSize = 50L * 1024 * 1024; // 50 MB

        /// <summary>
        /// Decompresses a byte array asynchronously, capped at <see cref="DefaultMaxDecompressedSize"/>.
        /// </summary>
        /// <param name="input">The byte array to decompress.</param>
        /// <param name="token">The <see cref="CancellationToken"/>.</param>
        /// <returns>The decompressed byte array.</returns>
        public static Task<byte[]> DecompressAsync(byte[] input, CancellationToken token = default)
        {
            return DecompressAsync(input, DefaultMaxDecompressedSize, token);
        }

        /// <summary>
        /// Decompresses a byte array asynchronously, rejecting it outright once the decompressed
        /// output would exceed <paramref name="maxOutputBytes"/> - closing a memory-exhaustion
        /// ("zip bomb") vector where a small compressed payload expands to a very large one.
        /// </summary>
        /// <param name="input">The byte array to decompress.</param>
        /// <param name="maxOutputBytes">Maximum allowed decompressed size, in bytes.</param>
        /// <param name="token">The <see cref="CancellationToken"/>.</param>
        /// <returns>The decompressed byte array.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the input is not a valid GZip stream, or the decompressed output exceeds <paramref name="maxOutputBytes"/>.</exception>
        public static async Task<byte[]> DecompressAsync(byte[] input, long maxOutputBytes, CancellationToken token = default)
        {
            try
            {
                using var source = new MemoryStream(input);
                using var result = new MemoryStream();
                using (var decompress = new GZipStream(source, CompressionMode.Decompress))
                {
                    var buffer = new byte[BufferSize];
                    long total = 0;
                    int read;
                    while ((read = await decompress.ReadAsync(buffer, token)) > 0)
                    {
                        total += read;
                        if (total > maxOutputBytes)
                        {
                            throw new InvalidOperationException($"Decompressed payload exceeds the configured limit of {maxOutputBytes} bytes.");
                        }
                        await result.WriteAsync(buffer.AsMemory(0, read), token);
                    }
                }
                return result.ToArray();
            }
            catch (InvalidDataException ex)
            {
                // Log the exception
                throw new InvalidOperationException("The input byte array is not a valid GZip stream.", ex);
            }
            catch (InvalidOperationException)
            {
                // Our own size-limit rejection - propagate the specific message unchanged.
                throw;
            }
            catch (Exception ex)
            {
                // Log the exception
                throw new InvalidOperationException("Failed to decompress the input byte array.", ex);
            }
        }

        /// <summary>
        /// Compresses a byte array asynchronously.
        /// </summary>
        /// <param name="input">The byte array to compress.</param>
        /// <param name="token">The <see cref="CancellationToken"/>.</param>
        /// <returns>The compressed byte array.</returns>
        public static async Task<byte[]> CompressAsync(byte[] input, CancellationToken token = default)
        {
            try
            {
                using var result = new MemoryStream();
                using (var compress = new GZipStream(result, CompressionLevel.Optimal))
                {
                    await compress.WriteAsync(input,token);
                }
                return result.ToArray();
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                throw new InvalidOperationException("Failed to compress the input byte array.", ex);
            }
        }
    }
}
