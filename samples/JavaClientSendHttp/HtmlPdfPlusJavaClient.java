// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the HtmlPdfPlus team
// https://github.com/FRACerqueira/HtmlPdfPlus
// ***************************************************************************************
//
// Minimal Java client for the HtmlPdfPlus server - no build tool, no dependency beyond the
// JDK itself (java.net.http.HttpClient + java.util.zip.GZIPOutputStream). The point of this
// sample is to show the exact wire format any non-.NET client must produce, not to be a
// polished library - a real integration would likely use a JSON library instead of the
// hand-rolled escaping below.
//
// Wire format (see HtmlPdfEndpointExtensions.MapHtmlPdfEndpoints and
// HtmlPdfClientInstance.CreateHttpContent on the .NET side for the authoritative version):
//   1) Build the request as JSON: {"Html":"...","Alias":"...","Config":null,
//      "Timeout":30000,"InputParam":null,"Mode":0,"SentAtUtc":null}
//      - Config:null lets the server fall back to its own configured page defaults.
//      - Mode is the RenderMode enum's underlying int: 0 = Html, 1 = Url.
//   2) gzip-compress the UTF-8 JSON bytes (standard RFC 1952 gzip - interoperable with
//      .NET's GZipStream, no custom framing).
//   3) POST the gzip bytes directly as the HTTP body with
//      Content-Type: application/octet-stream - no base64, no extra JSON-string wrapping.
//   4) A 200 response with Content-Type: application/pdf is the raw PDF bytes.
//      A non-2xx response is a JSON ErrorInfo body ({"code":...,"message":...}).
//
// Run the server first with the plain-HTTP profile so there's no TLS dev-certificate trust
// to set up across runtimes:
//   dotnet run --project samples/WebHtmlToPdf.GenericServer --launch-profile http
// (the server logs a harmless "Failed to determine the https port for redirect" warning on
// this profile - UseHttpsRedirection has no HTTPS endpoint to redirect to and no-ops; the
// request below still gets a normal response)
//
// Then compile and run this file (JDK 17+, for the text block below):
//   javac HtmlPdfPlusJavaClient.java
//   java HtmlPdfPlusJavaClient

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.zip.GZIPOutputStream;

public class HtmlPdfPlusJavaClient {

    private static final String SERVER_URL = "http://localhost:5042/GeneratePdf";

    public static void main(String[] args) throws IOException, InterruptedException {
        System.out.println("Example: minimal Java client for HtmlPdfPlus - hand-built wire format, no JSON/HTTP dependency beyond the JDK");
        System.out.println("Start the WebHtmlToPdf.GenericServer sample with the http profile first (see the header comment), then press Enter.");
        System.in.read();

        String html = htmlSample();
        String requestJson = buildRequestJson(html, "java-client-sample", 30000);
        byte[] compressed = gzip(requestJson.getBytes(StandardCharsets.UTF_8));

        HttpClient client = HttpClient.newHttpClient();
        HttpRequest request = HttpRequest.newBuilder()
                .uri(URI.create(SERVER_URL))
                .header("Content-Type", "application/octet-stream")
                .POST(HttpRequest.BodyPublishers.ofByteArray(compressed))
                .build();

        HttpResponse<byte[]> response = client.send(request, HttpResponse.BodyHandlers.ofByteArray());
        String contentType = response.headers().firstValue("Content-Type").orElse("");

        if (response.statusCode() == 200 && contentType.startsWith("application/pdf")) {
            Path outputPath = Path.of(System.getProperty("user.home"), "Desktop", "html2pdf-java.pdf");
            Files.write(outputPath, response.body());
            System.out.println("File PDF generated at " + outputPath);
        } else {
            // A real client would parse this JSON ErrorInfo body with a JSON library; printed
            // raw here to keep this sample dependency-free.
            System.out.println("HtmlPdfPlus error (HTTP " + response.statusCode() + "): "
                    + new String(response.body(), StandardCharsets.UTF_8));
        }
    }

    private static String buildRequestJson(String html, String alias, int timeoutMs) {
        return "{"
                + "\"Html\":\"" + escapeJson(html) + "\","
                + "\"Alias\":\"" + escapeJson(alias) + "\","
                + "\"Config\":null,"
                + "\"Timeout\":" + timeoutMs + ","
                + "\"InputParam\":null,"
                + "\"Mode\":0,"
                + "\"SentAtUtc\":null"
                + "}";
    }

    private static String escapeJson(String value) {
        StringBuilder sb = new StringBuilder(value.length());
        for (int i = 0; i < value.length(); i++) {
            char c = value.charAt(i);
            switch (c) {
                case '"' -> sb.append("\\\"");
                case '\\' -> sb.append("\\\\");
                case '\n' -> sb.append("\\n");
                case '\r' -> sb.append("\\r");
                case '\t' -> sb.append("\\t");
                default -> {
                    if (c < 0x20) {
                        sb.append(String.format("\\u%04x", (int) c));
                    } else {
                        sb.append(c);
                    }
                }
            }
        }
        return sb.toString();
    }

    private static byte[] gzip(byte[] data) throws IOException {
        ByteArrayOutputStream byteStream = new ByteArrayOutputStream();
        try (GZIPOutputStream gzipStream = new GZIPOutputStream(byteStream)) {
            gzipStream.write(data);
        }
        return byteStream.toByteArray();
    }

    private static String htmlSample() {
        return """
                <!DOCTYPE html>
                <html lang="en">
                <head>
                    <meta charset="UTF-8">
                    <title>HtmlPdfPlus Java Client</title>
                    <style>
                        body { font-family: Arial, sans-serif; margin: 40px; }
                        h1 { color: #4682b4; }
                    </style>
                </head>
                <body>
                    <h1>Generated from a Java client</h1>
                    <p>This PDF was produced by HtmlPdfPlusJavaClient.java, a dependency-free
                    demonstration of the HtmlPdfPlus wire format - no .NET runtime involved on
                    the client side.</p>
                </body>
                </html>
                """;
    }
}
