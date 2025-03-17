// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace HtmlPdfPlus.Shared.Core
{
    internal static class GZipHelper
    {
        public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        /// <summary>
        /// Decompresses a base64 encoded string.
        /// </summary>
        /// <param name="input">The base64 encoded string to decompress.</param>
        /// <returns>The decompressed string.</returns>
        public static string Decompress(string input)
        {
            try
            {
                byte[] compressed = Convert.FromBase64String(input);
                byte[] decompressed = Decompress(compressed);
                return Encoding.UTF8.GetString(decompressed);
            }
            catch (FormatException ex)
            {
                // Log the exception
                throw new InvalidOperationException("The input string is not a valid base64 string.", ex);
            }
            catch (InvalidOperationException ex)
            {
                // Log the exception
                throw new InvalidOperationException("Failed to decompress the input string.", ex);
            }
        }

        /// <summary>
        /// Compresses a string and returns a base64 encoded result.
        /// </summary>
        /// <param name="input">The string to compress.</param>
        /// <returns>The compressed and base64 encoded string.</returns>
        public static string Compress(string input)
        {
            try
            {
                byte[] encoded = Encoding.UTF8.GetBytes(input);
                byte[] compressed = Compress(encoded);
                return Convert.ToBase64String(compressed);
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                throw new InvalidOperationException("Failed to compress the input string.", ex);
            }
        }

        /// <summary>
        /// Decompresses a byte array.
        /// </summary>
        /// <param name="input">The byte array to decompress.</param>
        /// <returns>The decompressed byte array.</returns>
        public static byte[] Decompress(byte[] input)
        {
            try
            {
                using var source = new MemoryStream(input);
                using var result = new MemoryStream();
                using (var decompress = new GZipStream(source, CompressionMode.Decompress))
                {
                    decompress.CopyTo(result);
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
        /// Compresses a byte array.
        /// </summary>
        /// <param name="input">The byte array to compress.</param>
        /// <returns>The compressed byte array.</returns>
        public static byte[] Compress(byte[] input)
        {
            try
            {
                using var result = new MemoryStream();
                using (var compress = new GZipStream(result, CompressionMode.Compress))
                {
                    compress.Write(input, 0, input.Length);
                }
                return result.ToArray();
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                throw new InvalidOperationException("Failed to compress the input byte array.", ex);
            }
        }

        /// <summary>
        /// Compresses a request object to a base64 encoded string.
        /// </summary>
        /// <typeparam name="T">The type of the input parameter.</typeparam>
        /// <param name="alias">The alias for the request.</param>
        /// <param name="pageConfig">The PDF page configuration.</param>
        /// <param name="html">The HTML content.</param>
        /// <param name="timeout">The timeout value.</param>
        /// <param name="inputparam">The input parameter.</param>
        /// <returns>The compressed and base64 encoded request string.</returns>
        internal static string CompressRequest<T>(string? alias, PdfPageConfig? pageConfig, string html, int timeout, T? inputparam)
        {
            try
            {
                var request = new RequestHtmlPdf<T>(html, alias, pageConfig, timeout, inputparam);
                var json = JsonSerializer.Serialize(request, JsonOptions);
                return Compress(json);
            }
            catch (Exception ex)
            {
                // Log the exception
                throw new InvalidOperationException("Failed to compress the request object.", ex);
            }
        }

        /// <summary>
        /// Decompresses a base64 encoded request string to a request object.
        /// </summary>
        /// <typeparam name="T">The type of the input parameter.</typeparam>
        /// <param name="compressdata">The compressed and base64 encoded request string.</param>
        /// <returns>The decompressed request object.</returns>
        internal static RequestHtmlPdf<T> DecompressRequest<T>(string compressdata)
        {
            try
            {
                var json = Decompress(compressdata);
                var result = JsonSerializer.Deserialize<RequestHtmlPdf<T>>(json, JsonOptions);
                return result!;
            }
            catch (JsonException ex)
            {
                // Log the exception
                throw new InvalidOperationException("Failed to deserialize the decompressed JSON string.", ex);
            }
            catch (Exception ex)
            {
                // Log the exception
                throw new InvalidOperationException("Failed to decompress the request object.", ex);
            }
        }
    }
}
