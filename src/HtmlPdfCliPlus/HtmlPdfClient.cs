// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Text.Json;
using HtmlPdfCliPlus.Core;

namespace HtmlPdfPlus
{
    /// <summary>
    /// Fluent interface commands to perform client HTML to PDF conversion 
    /// </summary>
    public static class HtmlPdfClient
    {
        /// <summary>
        /// Options for disabling internal features.
        /// </summary>
        public static DisableOptionsHtmlToPdf DisableOptions { get; set; } = DisableOptionsHtmlToPdf.EnabledAllFeatures;

        /// <summary>
        /// Create an instance of Html to Pdf Client
        /// </summary>
        /// <param name="alias">Alias name for client</param>
        /// <returns><see cref="IHtmlPdfClient"/> instance.</returns>
        public static IHtmlPdfClient Create(string? alias = null)
        {
            return new HtmlPdfClientInstance(alias ?? string.Empty, DisableOptions);
        }

        /// <summary>
        /// Convert Response Data from server HtmlPdfPlus   
        /// </summary>
        /// <param name="dataresponse">Response data</param>
        /// <returns><see cref="HtmlPdfResult{T}"/></returns>
        public static HtmlPdfResult<byte[]> ToHtmlPdfResult(this string dataresponse)
        {
            return dataresponse.ToHtmlPdfResult<byte[]>();
        }

        /// <summary>
        /// Convert Response Data from server HtmlPdfPlus   
        /// </summary>
        /// <typeparam name="T">Type of result</typeparam>
        /// <param name="dataresponse">Response data</param>
        /// <returns><see cref="HtmlPdfResult{T}"/></returns>
        public static HtmlPdfResult<T> ToHtmlPdfResult<T>(this string dataresponse)
        {
            if (string.IsNullOrEmpty(dataresponse))
            {
                throw new ArgumentException("Response data cannot be null or empty", nameof(dataresponse));
            }

            return JsonSerializer.Deserialize<HtmlPdfResult<T>>(dataresponse, jsonoptions)!;
        }

        private static readonly JsonSerializerOptions jsonoptions = new()
        {
            PropertyNameCaseInsensitive = true
        };
    }
}

