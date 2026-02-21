using System.Net;

namespace UnitTests.Tooling;

public class CookieAwareMockHandler(HttpMessageHandler innerHandler, CookieContainer cookieContainer) : DelegatingHandler(innerHandler)
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Add cookies to request
        if (request.RequestUri != null)
        {
            var cookieHeader = cookieContainer.GetCookieHeader(request.RequestUri);
            if (!string.IsNullOrEmpty(cookieHeader))
            {
                request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
            }
        }

        // Send request through mock handler
        var response = await base.SendAsync(request, cancellationToken);

        // Extract cookies from response
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookieHeaders) && request.RequestUri != null)
        {
            foreach (var setCookie in setCookieHeaders)
            {
                cookieContainer.SetCookies(request.RequestUri, setCookie);
            }
        }

        return response;
    }
}
