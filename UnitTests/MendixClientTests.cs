using BrightonBins.Client;
using BrightonBins.Dtos;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using UnitTests.Tooling;

namespace UnitTests;

public class MendixClientTests
{
    [Fact]
    public async Task GetSchedule_Basic()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = handler.CreateClient();

        handler.SetupRequest(HttpMethod.Get, "https://enviroservices.brighton-hove.gov.uk/link/collections")
            .ReturnsResponse(HttpStatusCode.OK);

        handler.SetupRequestSequence(HttpMethod.Post, "https://enviroservices.brighton-hove.gov.uk/xas/")
            .ReturnsResponse(TestFileTools.GetFile("GetSessionData.json"))
            .ReturnsResponse(TestFileTools.GetFile("PostCodeSearch.json"))
            .ReturnsResponse(TestFileTools.GetFile("AddressSelection.json"));

        handler.SetupRequest(HttpMethod.Get, "https://enviroservices.brighton-hove.gov.uk/pages/en_GB/BartecCollective/Jobs_Get_Combined.page.xml").ReturnsResponse(TestFileTools.GetFile("JobsGetCombined.page.xml"));

        IMendixClient client = new MendixClient(httpClient);
        var res = await client.GetSchedule("BN1 8NT", 22058876);

        res.Should().NotBeNull();

        res.Where(a => a.Attributes["Collection_Date"].Value == "13/02/2026, 07:00").Should().NotBeEmpty();

        handler.VerifyAll();
    }

    [Fact]
    public async Task GetSchedule_Filtered()
    {
        var cookieContainer = new CookieContainer();
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = new HttpClient(new CookieAwareMockHandler(handler.Object, cookieContainer))
        {
            BaseAddress = new Uri("https://enviroservices.brighton-hove.gov.uk/")
        };

        handler.SetupRequest(HttpMethod.Get, "https://enviroservices.brighton-hove.gov.uk/link/collections")
            .ReturnsResponse(HttpStatusCode.OK);

        handler.SetupRequest(HttpMethod.Post, "https://enviroservices.brighton-hove.gov.uk/xas/",
            request => HasActionAsync<SessionDataRequestDto>(request, _ => true, "get_session_data", IsValidSessionDataRequest))
            .ReturnsResponse(TestFileTools.GetFile("GetSessionData.json"), configure: AddHeadersForSession);
        handler.SetupRequest(HttpMethod.Post, "https://enviroservices.brighton-hove.gov.uk/xas/",
            request => HasActionAsync<RuntimeOperationRequestDto>(request, AreHeadersValid, "runtimeOperation", IsValidPostCodeSearchRequest))
            .ReturnsResponse(TestFileTools.GetFile("PostCodeSearch.json"));
        handler.SetupRequest(HttpMethod.Post, "https://enviroservices.brighton-hove.gov.uk/xas/",
            request => HasActionAsync<RuntimeOperationRequestDto>(request, AreHeadersValid, "runtimeOperation", IsValidAddressSelectionRequest))
            .ReturnsResponse(TestFileTools.GetFile("AddressSelection.json"));

        handler.SetupRequest(HttpMethod.Get, "https://enviroservices.brighton-hove.gov.uk/pages/en_GB/BartecCollective/Jobs_Get_Combined.page.xml")
            .ReturnsResponse(TestFileTools.GetFile("JobsGetCombined.page.xml"));

        IMendixClient client = new MendixClient(httpClient);
        var res = await client.GetSchedule("BN1 8NT", 22058876);

        res.Should().NotBeNull();

        res.Where(a => a.Attributes["Collection_Date"].Value == "13/02/2026, 07:00").Should().NotBeEmpty();

        handler.VerifyAll();
    }

    private static readonly IEnumerable<string> _cookieValues = [
            "__Host-SessionTimeZoneOffset=0; Path=/; Secure; HttpOnly; SameSite=Strict",
            "__Host-XASSESSIONID=d311f779-18c5-415c-bd13-1b49b94800e8; Path=/; Secure; HttpOnly; SameSite=Strict",
            "xasid=0.f7b676b1-1fd9-45ad-b8dd-02259bec3e5d; Path=/; Secure; HttpOnly; SameSite=Strict",
            "__Host-DeviceType=Desktop; Path=/; Expires=Sat, 13 Feb 2027 16:32:31 GMT; Max-Age=31536000; Secure; HttpOnly; SameSite=Strict",
            "__Host-Profile=Responsive; Path=/; Expires=Sat, 13 Feb 2027 16:32:31 GMT; Max-Age=31536000; Secure; HttpOnly; SameSite=Strict"
            ];

    private static void AddHeadersForSession(HttpResponseMessage message)
    {
        message.Headers.Add("set-cookie", _cookieValues);
    }

    private static bool IsValidSessionDataRequest(SessionDataRequestDto dto) => true;

    private static bool IsValidPostCodeSearchRequest(RuntimeOperationRequestDto dto)
    {
        if (dto.OperationId != "tglPIXhcJ1abaQ1fhKJvYA")
            return false;

        if (dto.Params.Count != 1)
            return false;

        if (!dto.Params["Address"]["guid"].StartsWith(BHCCMendixConstants.BHCCThemeAddress))
            return false;

        if (dto.Changes.Count != 2)
            return false;

        if (ComparisonTools.GetKeyValue(dto.Changes, BHCCMendixConstants.BHCCThemeAddress).Value<string>("SearchString") != "BN1 8NT")
            return false;

        if (!ComparisonTools.HasKeys(dto.Changes, [BHCCMendixConstants.BHCCThemeAddress, BHCCMendixConstants.CollectionsCollection]))
            return false;

        if (dto.Objects.Length != 2)
            return false;

        if (!ComparisonTools.HasGuids(dto.Objects, [BHCCMendixConstants.BHCCThemeAddress, BHCCMendixConstants.CollectionsCollection]))
            return false;

        return true;
    }

    private static bool IsValidAddressSelectionRequest(RuntimeOperationRequestDto dto)
    {
        if (dto.OperationId != "DExhrgP93VCBOks+s8GjTQ")
            return false;

        if (dto.Params.Count != 1)
            return false;

        if (!dto.Params["Collection"]["guid"].StartsWith(BHCCMendixConstants.CollectionsCollection))
            return false;

        if (!ComparisonTools.HasKeys(dto.Changes, [BHCCMendixConstants.BHCCThemeAddress, BHCCMendixConstants.CollectionsCollection, BHCCMendixConstants.BHCCThemeAddressTempTable]))
            return false;

        if (dto.Changes.Count != 44)
            return false;

        var addressChanges = ComparisonTools.GetKeyValue(dto.Changes, BHCCMendixConstants.BHCCThemeAddress);
        if (addressChanges.Value<string>("SearchString") != "BN1 8NT")
            return false;

        if (!addressChanges.Value<string>("Collections.Collection_Address").StartsWith(BHCCMendixConstants.CollectionsCollection))
            return false;

        if (!addressChanges.Value<string>("BHCCTheme.AddressTemp_SelectedAddress").StartsWith(BHCCMendixConstants.UprnChangeElement))
            return false;

        var collectionChanges = ComparisonTools.GetKeyValue(dto.Changes, BHCCMendixConstants.CollectionsCollection);
        if (collectionChanges.Value<bool>("DisplayCollectionsButton") != true)
            return false;

        var uprnChanges = ComparisonTools.GetKeyValue(dto.Changes.Where(a => a.Value.ContainsKey("BHCCTheme.AddressTemp_SelectedAddress")).ToDictionary(x => x.Key, x => x.Value), BHCCMendixConstants.UprnChangeElement);
        if (!uprnChanges.Value<string>("BHCCTheme.AddressTemp_SelectedAddress").StartsWith(BHCCMendixConstants.BHCCThemeAddress))
            return false;

        if (dto.Objects.Length != 44)
            return false;

        if (!ComparisonTools.HasGuids(dto.Objects, [BHCCMendixConstants.BHCCThemeAddress, BHCCMendixConstants.CollectionsCollection, BHCCMendixConstants.BHCCThemeAddressTempTable]))
            return false;

        return true;
    }

    private static async Task<bool> HasActionAsync<T>(HttpRequestMessage message, Func<HttpRequestHeaders, bool> headers, string expectedAction, Func<T, bool> pred) where T : RequestDtoBase
    {
        var json = (JsonContent)message.Content!;
        var dto = (RequestDtoBase)json.Value!;

        return dto.Action == expectedAction && pred((T)dto) && headers(message.Headers);
    }

    private static bool AreHeadersValid(HttpRequestHeaders h)
    {
        var expectedCookies = string.Join(" ", _cookieValues.Select(c => c.Split(" ").First())).TrimEnd(';');
        return h.GetValues("x-csrf-token").SequenceEqual(["5f14fcf3-69da-49fe-b61e-d234188a48d5"]) && 
            h.GetValues("Cookie").SequenceEqual([expectedCookies]);
    }
}

file class CookieAwareMockHandler(HttpMessageHandler innerHandler, CookieContainer cookieContainer) : DelegatingHandler(innerHandler)
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
