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
public Task<HtmlPdfResult<byte[]>> Run(HttpClient httpClient, string endpoint, 
    CancellationToken token = default)
```

| parameter | description |
| --- | --- |
| httpClient | Instance of HttpClient. |
| endpoint | The endpoint for the HTTP client. |
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

### IHtmlPdfClient.Run&lt;Tin,Tout&gt; method (4 of 6)

#### Submit the HTML to convert to PDF in custom output via the SubmitHtmlToPdf function.

```csharp
public Task<HtmlPdfResult<Tout>> Run<Tin, Tout>(
    Func<byte[], CancellationToken, Task<HtmlPdfResult<Tout>>> submitHtmlToPdf, Tin? customData, 
    CancellationToken token = default)
```

| parameter | description |
| --- | --- |
| Tin | Type of input data. |
| Tout | Type of output data. |
| submitHtmlToPdf | Handler to function submit to server. |
| customData | Input data, for customizing HTML before converting to PDF on the server. |
| token | CancellationToken token. |

### Return Value

Returns HtmlPdfResult representing the asynchronous operation of converting HTML to PDF.

### Exceptions

| exception | condition |
| --- | --- |
| InvalidOperationException | Thrown when the empty Html source. |
| ArgumentNullException | Thrown when the submitHtmlToPdf function or customData is null. |

### See Also

* interface [IHtmlPdfClient](../IHtmlPdfClient.md)
* namespace [HtmlPdfPlus](../../HtmlPdfPlus.Client.md)

---

### IHtmlPdfClient.Run&lt;Tin,Tout&gt; method (5 of 6)

#### Submit the HTML to convert to PDF in custom output via POST HttpClient.

```csharp
public Task<HtmlPdfResult<Tout>> Run<Tin, Tout>(HttpClient httpClient, Tin? customData, 
    CancellationToken token = default)
```

| parameter | description |
| --- | --- |
| Tin | Type of input data. |
| Tout | Type of output data. |
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

### IHtmlPdfClient.Run&lt;Tin,Tout&gt; method (6 of 6)

#### Submit the HTML to convert to PDF in custom output via POST HttpClient.

```csharp
public Task<HtmlPdfResult<Tout>> Run<Tin, Tout>(HttpClient httpClient, string endpoint, 
    Tin? customData, CancellationToken token = default)
```

| parameter | description |
| --- | --- |
| Tin | Type of input data. |
| Tout | Type of output data. |
| httpClient | Instance of HttpClient. |
| endpoint | The endpoint for the HTTP client. |
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
