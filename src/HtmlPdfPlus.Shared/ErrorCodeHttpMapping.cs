// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

namespace HtmlPdfPlus
{
    /// <summary>
    /// Conventional HTTP status code for each <see cref="ErrorCode"/>, for hosts that expose
    /// <see cref="HtmlPdfResult{T}"/> over HTTP and want the status line itself to carry the
    /// failure category - not just a 200 with an embedded success flag. Returns a plain <see cref="int"/>
    /// so this carries no dependency on any specific web framework.
    /// </summary>
    public static class ErrorCodeHttpMapping
    {
        /// <summary>
        /// Maps an <see cref="ErrorCode"/> to a conventional HTTP status code.
        /// </summary>
        /// <param name="code">The error code to map.</param>
        /// <returns>The HTTP status code, as a plain integer.</returns>
        public static int ToHttpStatusCode(this ErrorCode code) => code switch
        {
            ErrorCode.InvalidRequest => 400,
            ErrorCode.Timeout => 504,
            // Canceled is always Retryable:true (see ErrorInfo.FromException) - a 400 would tell
            // a caller that only reads HTTP semantics (a proxy, a non-JSON-parsing client) not to
            // retry, contradicting that. 503 (temporary condition) matches the retryable intent
            // without reusing 504, which is reserved for the semantically distinct Timeout.
            ErrorCode.Canceled => 503,
            ErrorCode.PoolExhausted => 503,
            ErrorCode.RenderFailed => 502,
            ErrorCode.Internal => 500,
            _ => 500
        };
    }
}
