using XmlDocMarkdown.Core;

namespace XmlDocMarkdownGenerator
{
    internal class Program
    {
        //perform XmlDoc Markdown Generator
        static int Main()
        {
            if (Directory.Exists(@"..\..\..\..\docs\assemblies"))
            {
                Directory.Delete(@"..\..\..\..\docs\assemblies", true);
            }

            var args = new string[] { "HtmlPdfPlus.Client", @"..\..\..\..\docs\assemblies" };
            XmlDocMarkdownApp.Run(args);

            args = ["HtmlPdfPlus.Server", @"..\..\..\..\docs\assemblies"];
            XmlDocMarkdownApp.Run(args);

            args = ["HtmlPdfPlus.Shared", @"..\..\..\..\docs\assemblies"];
            XmlDocMarkdownApp.Run(args);

            return 0;
        }
    }
}
