![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### IHtmlPdfServerContext&lt;TIn,TOut&gt;.BeforePDF method
</br>


#### Function to enrich HTML or Url before performing conversion.

```csharp
public IHtmlPdfServerContext BeforePDF(
    Func<string, TIn?, CancellationToken, Task<string>> inputParam)
```

| parameter | description |
| --- | --- |
| inputParam | A function that takes a HTML or url, input data of type *TIn*, and a CancellationToken, and returns enriched HTML or url as a string. |

### Return Value

An instance of [`IHtmlPdfServer`](../IHtmlPdfServer-2.md).

### Exceptions

| exception | condition |
| --- | --- |
| ArgumentNullException | Thrown when *inputParam* is null. |

### See Also

* interface [IHtmlPdfServerContext&lt;TIn,TOut&gt;](../IHtmlPdfServerContext-2.md)
* namespace [HtmlPdfPlus](../../HtmlPdfPlus.Server.md)

<!-- DO NOT EDIT: generated by xmldocmd for HtmlPdfPlus.Server.dll -->
