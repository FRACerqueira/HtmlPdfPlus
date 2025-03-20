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
        /// Decompresses a byte array asynchronously.
        /// </summary>
        /// <param name="input">The byte array to decompress.</param>
        /// <param name="token">The <see cref="CancellationToken"/>.</param>
        /// <returns>The decompressed byte array.</returns>
        public static async Task<byte[]> DecompressAsync(byte[] input, CancellationToken token = default)
        {
            try
            {
                using var source = new MemoryStream(input);
                using var result = new MemoryStream();
                using (var decompress = new GZipStream(source, CompressionMode.Decompress))
                {
                    await decompress.CopyToAsync(result, BufferSize,token);
                }
                return result.ToArray();
            }
            catch (InvalidDataException ex)
            {
                // Log the exception
                throw new InvalidOperationException("The input byte array is not a valid GZip stream.", ex);
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
