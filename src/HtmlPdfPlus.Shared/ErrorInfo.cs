// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.Text.Json.Serialization;

namespace HtmlPdfPlus
{
    /// <summary>
    /// Structured, serializable description of a <see cref="HtmlPdfResult{T}"/> failure.
    /// </summary>
    /// <remarks>
    /// Replaces returning a raw <see cref="Exception"/>: an <see cref="Exception"/> that was
    /// actually thrown can fail to serialize with <c>System.Text.Json</c> (its <c>TargetSite</c>
    /// is not supported), and even when serialization succeeds, <see cref="Exception.Message"/>
    /// has no public setter so the original message does not survive a round-trip. <see cref="ErrorInfo"/>
    /// is a plain DTO with no such limitation, and <see cref="Code"/> gives any client -
    /// regardless of language - a stable value to branch on instead of matching .NET exception types.
    /// </remarks>
    public sealed class ErrorInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorInfo"/> class.
        /// </summary>
        /// <param name="code">The stable failure classification.</param>
        /// <param name="message">A human-readable description of the failure.</param>
        /// <param name="retryable"><c>true</c> if retrying the same request may succeed.</param>
        [JsonConstructor]
        public ErrorInfo(ErrorCode code, string message, bool retryable = false)
        {
            Code = code;
            Message = message ?? string.Empty;
            Retryable = retryable;
        }

        /// <summary>
        /// Gets the stable failure classification.
        /// </summary>
        public ErrorCode Code { get; }

        /// <summary>
        /// Gets a human-readable description of the failure.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets a value indicating whether retrying the same request may succeed.
        /// </summary>
        public bool Retryable { get; }

        /// <summary>
        /// Builds an <see cref="ErrorInfo"/> from a caught exception, classifying well-known
        /// .NET exception types into a stable <see cref="ErrorCode"/>.
        /// </summary>
        /// <param name="exception">The exception to classify.</param>
        /// <returns>The corresponding <see cref="ErrorInfo"/>.</returns>
        public static ErrorInfo FromException(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            return exception switch
            {
                TimeoutException => new ErrorInfo(ErrorCode.Timeout, exception.Message, retryable: true),
                OperationCanceledException => new ErrorInfo(ErrorCode.Canceled, exception.Message, retryable: true),
                ArgumentException or InvalidOperationException => new ErrorInfo(ErrorCode.InvalidRequest, exception.Message),
                _ => new ErrorInfo(ErrorCode.Internal, exception.Message)
            };
        }

        /// <inheritdoc />
        public override string ToString() => $"{Code}: {Message}";
    }
}
