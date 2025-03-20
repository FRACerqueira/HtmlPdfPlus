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
        }
}
