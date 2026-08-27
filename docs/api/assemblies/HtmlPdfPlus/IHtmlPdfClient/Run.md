![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### IHtmlPdfClient.Run method (1 of 6)
</br>


#### Submit the HTML to convert to PDF in byte[] by the SubmitHtmlToPdf function.

```csharp
public Task<HtmlPdfResult<byte[]>> Run(
    Func<byte[], CancellationToken, Task<HtmlPdfResult<byte[]>>> submitHtmlToPdf, 
    CancellationToken token = default)
```

| parameter | description |
| --- | --- |
| submitHtmlToPdf | Handler to function submit to server. |
| token | CancellationToken token. |

### Return Value

Returns bytes[] from HtmlPdfResult representing the asynchronous operation of converting HTML to PDF.

### Exceptions

| exception | condition |
| --- | --- |
| InvalidOperationException | Thrown when the empty Html source. |
| ArgumentNullException | Thrown when the submitHtmlToPdf function is null. |

### See Also

* interface [IHtmlPdfClient](../IHtmlPdfClient.md)
* namespace [HtmlPdfPlus](../../HtmlPdfPlus.Client.md)

---

### IHtmlPdfClient.Run method (2 of 6)

#### Submit the HTML to convert to PDF in byte[] via POST HttpClient.

```csharp
public Task<HtmlPdfResult<byte[]>> Run(HttpClient httpClient, CancellationToken token = default)
```

| parameter | description |
| --- | --- |
| httpClient | Instance of HttpClient. |
| token | CancellationToken token. |

### Return Value

Returns bytes[] from HtmlPdfResult representing the asynchronous operation of converting HTML to PDF.

### Exceptions

| exception | condition |
| --- | --- |
| InvalidOperationException | Thrown when the empty Html source. |

### See Also

* interface [IHtmlPdfClient](../IHtmlPdfClient.md)
* namespace [HtmlPdfPlus](../../HtmlPdfPlus.Client.md)

---

### IHtmlPdfClient.Run method (3 of 6)

#### Submit the HTML to convert to PDF in byte[] via POST HttpClient.

```csharp
public Task<HtmlPdfResult<byte[]>> Run(HttpClient httpClient, string? endpoint, 
    CancellationToken token = default)
```

| parameter | description |
| --- | --- |
| httpClient | Instance of HttpClient. |
| endpoint | The endpoint for the HTTP client, or `null`/empty to POST to BaseAddress directly. |
| token | CancellationToken token. |

### Return Value

Returns bytes[] from HtmlPdfResult representing the asynchronous operation of converting HTML to PDF.

### Exceptions

| exception | condition |
| --- | --- |
| InvalidOperationException | Thrown when the empty Html source. |

### See Also

* interface [IHtmlPdfClient](../IHtmlPdfClient.md)
* namespace [HtmlPdfPlus](../../HtmlPdfPlus.Client.md)

---

### IHtmlPdfClient.Run&lt;TIn,TOut&gt; method (4 of 6)

#### Submit the HTML to convert to PDF in custom output via the SubmitHtmlToPdf function.

```csharp
public Task<HtmlPdfResult<TOut>> Run<TIn, TOut>(
    Func<byte[], CancellationToken, Task<HtmlPdfResult<TOut>>> submitHtmlToPdf, TIn? customData, 
    CancellationToken token = default)
```

| parameter | description |
| --- | --- |
| TIn | Type of input data. |
| TOut | Type of output data. |
| submitHtmlToPdf | Handler to function submit to server. |
| customData | Input data, for customizing HTML before converting to PDF on the server. |
| token | CancellationToken token. |

### Return Value

Returns HtmlPdfResult representing the asynchronous operation of converting HTML to PDF.

### Exceptions

| exception | condition |
| --- | --- |
| InvalidOperationException | Thrown when the empty Html source. |
| ArgumentNullException | Thrown when the submitHtmlToPdf function is null. |

### See Also

* interface [IHtmlPdfClient](../IHtmlPdfClient.md)
* namespace [HtmlPdfPlus](../../HtmlPdfPlus.Client.md)

---

### IHtmlPdfClient.Run&lt;TIn,TOut&gt; method (5 of 6)

#### Submit the HTML to convert to PDF in custom output via POST HttpClient.

```csharp
public Task<HtmlPdfResult<TOut>> Run<TIn, TOut>(HttpClient httpClient, TIn? customData, 
    CancellationToken token = default)
```

| parameter | description |
| --- | --- |
| TIn     | Type of input data. |
| TOut | Type of output data. |
| httpClient | Instance of HttpClient. |
| customData | Input data, for customizing HTML before converting to PDF on the server. |
| token | CancellationToken token. |

### Return Value

Returns HtmlPdfResult representing the asynchronous operation of converting HTML to PDF.

### Exceptions

| exception | condition |
| --- | --- |
| InvalidOperationException | Thrown when the empty Html source. |

### See Also

* interface [IHtmlPdfClient](../IHtmlPdfClient.md)
* namespace [HtmlPdfPlus](../../HtmlPdfPlus.Client.md)

---

### IHtmlPdfClient.Run&lt;TIn,TOut&gt; method (6 of 6)

#### Submit the HTML to convert to PDF in custom output via POST HttpClient.

```csharp
public Task<HtmlPdfResult<TOut>> Run<TIn, TOut>(HttpClient httpClient, string? endpoint, 
    TIn? customData, CancellationToken token = default)
```

| parameter | description |
| --- | --- |
| TIn | Type of input data. |
| TOut | Type of output data. |
| httpClient | Instance of HttpClient. |
| endpoint | The endpoint for the HTTP client, or `null`/empty to POST to BaseAddress directly. |
| customData | Input data, for customizing HTML before converting to PDF on the server. |
| token | CancellationToken token. |

### Return Value

Returns HtmlPdfResult representing the asynchronous operation of converting HTML to PDF.

### Exceptions

| exception | condition |
| --- | --- |
| InvalidOperationException | Thrown when the empty Html source. |

### See Also

* interface [IHtmlPdfClient](../IHtmlPdfClient.md)
* namespace [HtmlPdfPlus](../../HtmlPdfPlus.Client.md)

<!-- DO NOT EDIT: generated by xmldocmd for HtmlPdfPlus.Client.dll -->
