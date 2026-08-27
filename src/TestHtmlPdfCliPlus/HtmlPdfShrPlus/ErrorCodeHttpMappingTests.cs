// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus;

namespace TestHtmlPdfPlus.HtmlPdfShrPlus
{
    public class ErrorCodeHttpMappingTests
    {
        [Fact]
        public void Canceled_MapsTo_ServiceUnavailable_ConsistentWithItsRetryableIntent()
        {
            // ErrorInfo.FromException always sets Retryable:true for a canceled operation - a 400
            // (conventionally "don't retry as-is") would contradict that for a caller that only
            // reads HTTP semantics (a proxy, a non-JSON-parsing client).
            Assert.Equal(503, ErrorCode.Canceled.ToHttpStatusCode());
        }

        [Fact]
        public void Canceled_And_Timeout_MapToDistinctStatusCodes()
        {
            // Canceled ("a token other than the timeout") and Timeout are a deliberately distinct
            // classification (see ErrorCode's own doc comments) - they must not collapse onto the
            // same HTTP status.
            Assert.NotEqual(ErrorCode.Timeout.ToHttpStatusCode(), ErrorCode.Canceled.ToHttpStatusCode());
        }
    }
}
