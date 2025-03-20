// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************

using System.IO.Compression;
using System.Text;
using HtmlPdfPlus;

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
            var error = new Exception("Test Exception");

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
