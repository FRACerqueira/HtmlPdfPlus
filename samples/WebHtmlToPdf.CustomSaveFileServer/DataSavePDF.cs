namespace WebHtmlToPdf.CustomSaveFileServer
{
    public sealed class DataSavePDF
    { 
        public string Filename { get; set; }  = string.Empty;
        public string Folder { get; set; } = string.Empty;
        public string ConnectionProvider { get; set; } = string.Empty;
    }
}
