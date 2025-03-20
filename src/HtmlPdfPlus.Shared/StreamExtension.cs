// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

namespace HtmlPdfPlus
{
    /// <summary>
    ///  Extend function for <see cref="Stream"/>
    /// </summary>
    public static class StreamExtension
    {
        /// <summary>
        /// Read the stream and return the content as a byte[]
        /// </summary>
        /// <param name="stream">The stream</param>
        /// <returns></returns>
        public static async Task<byte[]> ReadToBytesAsync(this Stream stream)
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
