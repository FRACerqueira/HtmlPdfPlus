// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using HtmlPdfPlus;
using HtmlPdfPlus.Shared.Core;

namespace TestHtmlPdfPlus.HtmlPdfShrPlus
{
    public class HtmlPdfResultTests
    {
        [Fact]
        public void Constructor_ShouldInitializeProperties()
        {
            // Arrange
            var isSuccess = true;
            var bufferDrained = false;
            var elapsedTime = TimeSpan.FromSeconds(1);
            var outputData = "Test Data";
            var error = new ErrorInfo(ErrorCode.Internal, "Test Exception");

            // Act
            var result = new HtmlPdfResult<string>(isSuccess, bufferDrained, elapsedTime, outputData, error);

            // Assert
            Assert.Equal(isSuccess, result.IsSuccess);
            Assert.Equal(bufferDrained, result.BufferDrained);
            Assert.Equal(elapsedTime, result.ElapsedTime);
            Assert.Equal(outputData, result.OutputData);
            Assert.Equal(error, result.Error);
        }

        [Fact]
        public void Error_JsonRoundTrip_PreservesCodeMessageAndRetryable_EvenForAThrownException()
        {
            // Given: an ErrorInfo built from an exception that was actually thrown (not just
            // constructed) - the scenario where the previous Exception-based Error crashed
            // System.Text.Json (TargetSite) or, when it didn't crash, lost the original Message
            // on deserialization because Exception.Message has no public setter.
            Exception caught;
            try
            {
                throw new InvalidOperationException("boom - the real message must survive");
            }
            catch (Exception ex)
            {
                caught = ex;
            }
            var original = new HtmlPdfResult<byte[]>(false, false, TimeSpan.FromMilliseconds(5), null, ErrorInfo.FromException(caught));

            // When: serialized and deserialized, exactly as it travels over the wire.
            var json = JsonSerializer.Serialize(original, GZipHelper.JsonOptions);
            var roundtripped = JsonSerializer.Deserialize<HtmlPdfResult<byte[]>>(json, GZipHelper.JsonOptions);

            // Then: no crash, and Code/Message/Retryable all survive intact.
            Assert.NotNull(roundtripped);
            Assert.NotNull(roundtripped!.Error);
            Assert.Equal(ErrorCode.InvalidRequest, roundtripped.Error!.Code);
            Assert.Equal("boom - the real message must survive", roundtripped.Error.Message);
            Assert.False(roundtripped.Error.Retryable);
        }

        [Fact]
        public void DecompressOutputData_ShouldDecompressByteArray()
        {
            // Arrange
            var isSuccess = true;
            var bufferDrained = false;
            var elapsedTime = TimeSpan.FromSeconds(1);
            var originalData = Encoding.UTF8.GetBytes("Test Data");
            var compressedData = Compress(originalData);
            var result = new HtmlPdfResult<byte[]>(isSuccess, bufferDrained, elapsedTime, compressedData);

            // Act
            var decompressedResult = result.DecompressOutputData();

            // Assert
            Assert.Equal(originalData, decompressedResult.OutputData);
        }

        [Fact]
        public void DecompressOutputData_ShouldThrowInvalidOperationException_WhenOutputDataIsNotByteArray()
        {
            // Arrange
            var isSuccess = true;
            var bufferDrained = false;
            var elapsedTime = TimeSpan.FromSeconds(1);
            var outputData = "Test Data";
            var result = new HtmlPdfResult<string>(isSuccess, bufferDrained, elapsedTime, outputData);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => result.DecompressOutputData());
        }

        private byte[] Compress(byte[] data)
        {
            using (var memoryStream = new System.IO.MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
                {
                    gzipStream.Write(data, 0, data.Length);
                }
                return memoryStream.ToArray();
            }
        }
    }
}
