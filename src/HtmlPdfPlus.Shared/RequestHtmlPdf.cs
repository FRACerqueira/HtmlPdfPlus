﻿// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Text;
using System.Text.Json;
using HtmlPdfPlus.Shared.Core;
using NUglify;

namespace HtmlPdfPlus
{
    /// <summary>
    /// Request data to convert Html to PDF
    /// </summary>
    /// <typeparam name="T">Input type</typeparam>
    /// <remarks>
    /// Request data to convert Html to PDF with all data
    /// </remarks>
    public sealed class RequestHtmlPdf<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestHtmlPdf{T}"/> class.
        /// </summary>
        /// <param name="html">The Html/url to render on browser. Html is required and must not be empty.</param>
        /// <param name="alias">The alias for request and must not be null</param>
        /// <param name="config">The Config PDF page. <see cref="PdfPageConfig"/></param>
        /// <param name="timeout">The timeout convert (default 30000ms) and value must be greater than zero</param>
        /// <param name="inputparam">The input parameter used in BeforePDF and AfterPDF for custom action at server</param>
        /// <exception cref="ArgumentException">Thrown when html is null or empty, or timeout is less than or equal to zero</exception>
        public RequestHtmlPdf(string html, string? alias = null, PdfPageConfig? config = null, int timeout = 30000, T? inputparam = default)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                throw new ArgumentException("Html is required and must not be empty.", nameof(html));
            }

            if (timeout <= 0)
            {
                throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));
            }

            Html = html;
            Alias = alias ?? string.Empty;
            Config = config;
            Timeout = timeout;
            InputParam = inputparam;
        }

        /// <summary>
        /// Gets the alias for request.
        /// </summary>
        public string? Alias { get; }

        /// <summary>
        /// Gets the Config PDF page. <see cref="PdfPageConfig"/>
        /// </summary>
        public PdfPageConfig? Config { get; internal set; }

        /// <summary>
        /// Gets the Html/url to render on browser. Html/url is required (must not be empty).
        /// </summary>
        public string Html { get; private set; }

        /// <summary>
        /// Gets the timeout convert (default 30000ms).
        /// </summary>
        public int Timeout { get; }

        /// <summary>
        /// Gets the input parameter used in BeforePDF and AfterPDF for custom action at server.
        /// </summary>
        public T? InputParam { get; }

        /// <summary>
        /// Changes the Html to render on browser.
        /// </summary>
        /// <param name="value">The Html value</param>
        /// <param name="minify">Minify content value</param>
        /// <exception cref="ArgumentException">Thrown when value is null or empty</exception>
        public void ChangeHtml(string value, bool minify)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Html is required and must not be empty.", nameof(value));
            }
            if (minify)
            {
                Html = Uglify.Html(value).Code;
            }
            else
            {
                Html = value;
            }
        }

        /// <summary>
        /// Returns a byte[] of JSON string representation of the <see cref="RequestHtmlPdf{T}"/>.
        /// </summary>
        /// <returns>Byte[] representation</returns>
        public byte[] ToBytes()
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this));
        }

        /// <summary>
        /// Returns a a compressed (gzip) byte[] of JSON string representation of the <see cref="RequestHtmlPdf{T}"/>.
        /// </summary>
        /// <returns>Byte[] compressed representation</returns>
        public async Task<byte[]> ToBytesCompress()
        {
            return await GZipHelper.CompressAsync(ToBytes());
        }

        /// <summary>
        /// Returns a <see cref="RequestHtmlPdf{T}"/> from a byte[].
        /// </summary>
        /// <param name="value">The <see cref="byte"/> array.</param>
        /// <returns><see cref="RequestHtmlPdf{T}"/>.</returns>
        public static RequestHtmlPdf<T> FromBytes(byte[] value)
        {
            return JsonSerializer.Deserialize<RequestHtmlPdf<T>>(Encoding.UTF8.GetString(value))!;
        }

        /// <summary>
        /// Returns a <see cref="RequestHtmlPdf{T}"/> from a compressed byte[].
        /// </summary>
        /// <param name="value">The <see cref="byte"/> array compressed.</param>
        /// <returns><see cref="RequestHtmlPdf{T}"/>.</returns>
        public static RequestHtmlPdf<T> FromBytesCompress(byte[] value)
        {
            return JsonSerializer.Deserialize<RequestHtmlPdf<T>>(Encoding.UTF8.GetString(GZipHelper.DecompressAsync(value).Result))!;
        }
    }
}
