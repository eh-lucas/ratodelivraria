namespace Sherlock.Business.Core.Scrapers.Common;

internal static class BrowserRequestFactory
{
    private const string ChromeUserAgent =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

    public static HttpRequestMessage Create(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", ChromeUserAgent);
        request.Headers.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
        request.Headers.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
        request.Headers.Add("Accept-Encoding", "gzip, deflate");
        return request;
    }
}
