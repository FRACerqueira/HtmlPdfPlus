// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Text.Json.Serialization;
using HtmlPdfPlus.Shared.Core;

namespace HtmlPdfPlus
{
    /// <summary>
    /// Result of converting Html to PDF
    /// </summary>
    /// <typeparam name="T">Type of output data</typeparam>
    public sealed class HtmlPdfResult<T>
    {
        /// <summary>
        /// Create instance <see cref="HtmlPdfResult{T}"/> 
        /// </summary>
        /// <param name="isSuccess">If the conversion was successful</param>
        /// <param name="bufferDrained">If the time limit for acquiring a page has been reached</param>
        /// <param name="elapsedTime">Time taken to convert html to PDF</param>
        /// <param name="outputData">Output custom data or PDF in byte[]</param>
        /// <param name="error">The exception during conversion. <see cref="Exception"/></param>
        [JsonConstructor]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = "by design")]
        public HtmlPdfResult(bool isSuccess, bool bufferDrained, TimeSpan elapsedTime, T? outputData, Exception? error = null)
        {
            IsSuccess = isSuccess;
            BufferDrained = bufferDrained;
            ElapsedTime = elapsedTime;
            OutputData = outputData;
            Error = error;
        }

        /// <summary>
        /// The exception during conversion. <see cref="Exception"/>
        /// </summary>
        public Exception? Error { get; }

        /// <summary>
        /// If the conversion was successful
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// If the time limit for acquiring a page has been reached
        /// </summary>
        public bool BufferDrained { get; }

        /// <summary>
        /// Time taken to convert html to PDF
        /// </summary>
        public TimeSpan ElapsedTime { get; }

        /// <summary>
        /// Output custom data or PDF in byte[]
        /// </summary>
        public T? OutputData { get; internal set; }

        /// <summary>
        /// Decompress OutputData when type is byte[]
        /// </summary>
        /// <returns>The <see cref="HtmlPdfResult{T}"/> with OutputData Decompressed when type is byte[]</returns>
        /// <exception cref="InvalidOperationException">OutputData is not byte[]</exception>
        public HtmlPdfResult<T> DecompressOutputData()
        {
            if (OutputData is byte[] data)
            {
                OutputData = (T)(object)GZipHelper.DecompressAsync(data).Result;
                return this;
            }
            throw new InvalidOperationException("OutputData is not byte[]");
        }
    }
}
