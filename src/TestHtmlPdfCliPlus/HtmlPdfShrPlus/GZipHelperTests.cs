// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using HtmlPdfPlus.Shared.Core;
using System.Text;

namespace TestHtmlPdfPlus.HtmlPdfShrPlus
{
        public class GZipHelperTests
        {
            [Fact]
            public async Task CompressAsync_ValidInput_CompressesData()
            {
                // Arrange
                var input = Encoding.UTF8.GetBytes("Hello, World!");
                var cancellationToken = CancellationToken.None;

                // Act
                var compressedData = await GZipHelper.CompressAsync(input, cancellationToken);

                // Assert
                Assert.NotNull(compressedData);
                Assert.NotEqual(input, compressedData);
            }

            [Fact]
            public async Task DecompressAsync_ValidInput_DecompressesData()
            {
                // Arrange
                var input = Encoding.UTF8.GetBytes("Hello, World!");
                var cancellationToken = CancellationToken.None;
                var compressedData = await GZipHelper.CompressAsync(input, cancellationToken);

                // Act
                var decompressedData = await GZipHelper.DecompressAsync(compressedData, cancellationToken);

                // Assert
                Assert.NotNull(decompressedData);
                Assert.Equal(input, decompressedData);
            }

            [Fact]
            public async Task DecompressAsync_InvalidInput_ThrowsInvalidOperationException()
            {
                // Arrange
                var invalidInput = new byte[] { 0xAF, 0x8B, 0x08 }; // Invalid GZip header
                var cancellationToken = CancellationToken.None;

                // Act & Assert
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => GZipHelper.DecompressAsync(invalidInput, cancellationToken));
                Assert.Equal("The input byte array is not a valid GZip stream.", exception.Message);
            }

            [Fact]
            public async Task DecompressAsync_WithinLimit_DecompressesData()
            {
                // Arrange
                var input = Encoding.UTF8.GetBytes("Hello, World!");
                var compressedData = await GZipHelper.CompressAsync(input, CancellationToken.None);

                // Act
                var decompressedData = await GZipHelper.DecompressAsync(compressedData, maxOutputBytes: input.Length, CancellationToken.None);

                // Assert
                Assert.Equal(input, decompressedData);
            }

            [Fact]
            public async Task DecompressAsync_ExceedsLimit_ThrowsInvalidOperationException()
            {
                // Arrange: a payload that decompresses to well more than the configured cap.
                var input = Encoding.UTF8.GetBytes(new string('a', 10_000));
                var compressedData = await GZipHelper.CompressAsync(input, CancellationToken.None);

                // Act & Assert
                var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                    () => GZipHelper.DecompressAsync(compressedData, maxOutputBytes: 100, CancellationToken.None));
                Assert.Contains("exceeds the configured limit", exception.Message);
            }

            [Fact]
            public async Task DecompressAsync_PropagatesOperationCanceledException_WhenTokenIsCanceled()
            {
                // Arrange: a valid compressed payload and an already-canceled token, so the read
                // loop's first ReadAsync observes the cancellation deterministically instead of
                // racing a real async yield point.
                var input = Encoding.UTF8.GetBytes(new string('a', 10_000));
                var compressedData = await GZipHelper.CompressAsync(input, CancellationToken.None);
                using var cts = new CancellationTokenSource();
                cts.Cancel();

                // Act & Assert: must propagate as an OperationCanceledException (Stream.ReadAsync
                // actually throws the subclass TaskCanceledException for an already-canceled
                // token, so ThrowsAnyAsync - not the exact-type ThrowsAsync - is the correct
                // assertion here) so callers classify it as Canceled/retryable, instead of being
                // wrapped into InvalidOperationException and misreported as a malformed/
                // non-retryable request.
                await Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => GZipHelper.DecompressAsync(compressedData, cts.Token));
            }
        }
}
