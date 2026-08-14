![HtmlPdfPLus Logo](https://raw.githubusercontent.com/FRACerqueira/HtmlPdfPLus/refs/heads/main/docs/images/iconsmall.png)

### FAQ

**Q: What browsers are supported for PDF generation?**

A: Currently, only the Chromium browser is supported for the PDF API - see [Project Description](../../README.md#project-description) for why.

**Q: What init args does the browser use for speed and reduced resource usage?**

A: `HtmlPdfPlus.Server` starts Chromium with `--run-all-compositor-stages-before-draw --disable-dev-shm-usage -disable-setuid-sandbox --no-sandbox` when no argument value is passed.

**Q: Can I customize the PDF settings?**

A: Yes - page size, margins, headers and footers are all configurable. See [PdfPageConfig](../api/assemblies/HtmlPdfPlus/PdfPageConfig.md) in the API reference.

**Q: Is there support for asynchronous operations?**

A: Yes, the entire API is asynchronous (`Task`-based).

**Q: How can I contribute to the project?**

A: See [Contributing](../../CONTRIBUTING.md).

### See Also
* [Main README](../../README.md)
* [Guide index](index.md)
* [How-To](howto/README.md)
