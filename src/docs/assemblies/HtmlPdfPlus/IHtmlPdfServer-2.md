![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### IHtmlPdfServer&lt;TIn,TOut&gt; interface
</br>


#### Fluent interface commands to perform HTML to PDF conversion.

```csharp
public interface IHtmlPdfServer<TIn, TOut>
```

| parameter | description |
| --- | --- |
| TIn | Type of input data. |
| TOut | Type of output data. |

### Members

| name | description |
| --- | --- |
| [AfterPDF](IHtmlPdfServer-2/AfterPDF.md)(…) | Function to transform to a new output type after performing HTML to PDF conversion. |
| [BeforePDF](IHtmlPdfServer-2/BeforePDF.md)(…) | Function to enrich HTML before performing HTML to PDF conversion. |
| [Run](IHtmlPdfServer-2/Run.md)(…) | Perform HTML to PDF conversion. |

### See Also

* namespace [HtmlPdfPlus](../HtmlPdfPlus.Server.md)

<!-- DO NOT EDIT: generated by xmldocmd for HtmlPdfPlus.Server.dll -->
