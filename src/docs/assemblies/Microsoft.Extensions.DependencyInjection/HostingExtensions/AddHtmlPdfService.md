![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### HostingExtensions.AddHtmlPdfService method (1 of 3)
</br>


#### Adds HtmlPdf Server to the IServiceCollection.

```csharp
public static IServiceCollection AddHtmlPdfService(this IServiceCollection serviceCollection, 
    Action<IHtmlPdfSrvBuilder>? config = null, string? sourceAlias = null)
```

| parameter | description |
| --- | --- |
| serviceCollection | The service collection. |
| config | An action to customize HtmlPdf Server configuration. |
| sourceAlias | Alias for this instance. If empty, uses the log's CategoryName property if it exists or empty. |

### Return Value

The IServiceCollection instance.

### See Also

* interface [IHtmlPdfSrvBuilder](../../HtmlPdfPlus/IHtmlPdfSrvBuilder.md)
* class [HostingExtensions](../HostingExtensions.md)
* namespace [Microsoft.Extensions.DependencyInjection](../../HtmlPdfPlus.Server.md)

---

### HostingExtensions.AddHtmlPdfService&lt;TOut&gt; method (2 of 3)

#### Adds HtmlPdf Server to the IServiceCollection.

```csharp
public static IServiceCollection AddHtmlPdfService<TOut>(this IServiceCollection serviceCollection, 
    Action<IHtmlPdfSrvBuilder>? config = null, string? sourceAlias = null)
```

| parameter | description |
| --- | --- |
| TOut | The type of the output parameter. |
| serviceCollection | The service collection. |
| config | An action to customize HtmlPdf Server configuration. |
| sourceAlias | Alias for this instance. If empty, uses the log's CategoryName property if it exists or empty. |

### Return Value

The IServiceCollection instance.

### See Also

* interface [IHtmlPdfSrvBuilder](../../HtmlPdfPlus/IHtmlPdfSrvBuilder.md)
* class [HostingExtensions](../HostingExtensions.md)
* namespace [Microsoft.Extensions.DependencyInjection](../../HtmlPdfPlus.Server.md)

---

### HostingExtensions.AddHtmlPdfService&lt;TIn,TOut&gt; method (3 of 3)

#### Adds HtmlPdf Server to the IServiceCollection.

```csharp
public static IServiceCollection AddHtmlPdfService<TIn, TOut>(
    this IServiceCollection serviceCollection, Action<IHtmlPdfSrvBuilder>? config = null, 
    string? sourceAlias = null)
```

| parameter | description |
| --- | --- |
| TIn | The type of the input parameter. |
| TOut | The type of the output parameter. |
| serviceCollection | The service collection. |
| config | An action to customize HtmlPdf Server configuration. |
| sourceAlias | Alias for this instance. If empty, uses the log's CategoryName property if it exists or empty. |

### Return Value

The IServiceCollection instance.

### Exceptions

| exception | condition |
| --- | --- |
| ArgumentNullException | Thrown when the service collection is null. |

### See Also

* interface [IHtmlPdfSrvBuilder](../../HtmlPdfPlus/IHtmlPdfSrvBuilder.md)
* class [HostingExtensions](../HostingExtensions.md)
* namespace [Microsoft.Extensions.DependencyInjection](../../HtmlPdfPlus.Server.md)

<!-- DO NOT EDIT: generated by xmldocmd for HtmlPdfPlus.Server.dll -->
