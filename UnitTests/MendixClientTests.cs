using BrightonBins.Client;
using BrightonBins.Dtos;
using FluentAssertions;
using Moq;
using Moq.Contrib.HttpClient;
using System.Net.Http.Json;

namespace UnitTests;

public class MendixClientTests
{
    [Fact]
    public async Task GetSchedule_Basic()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = handler.CreateClient();

        handler.SetupRequest(HttpMethod.Get, "https://enviroservices.brighton-hove.gov.uk/link/collections")
            .ReturnsResponse(System.Net.HttpStatusCode.OK);

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

    const string CollectionsCollection = "32088147";
    const string BHCCThemeAddress = "309622";
    const string BHCCThemeAddressTempTable = "1491819";

    [Fact]
    public async Task GetSchedule_Filtered()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = handler.CreateClient();

        handler.SetupRequest(HttpMethod.Get, "https://enviroservices.brighton-hove.gov.uk/link/collections")
            .ReturnsResponse(System.Net.HttpStatusCode.OK);

        handler.SetupRequest(HttpMethod.Post, "https://enviroservices.brighton-hove.gov.uk/xas/",
            request => HasActionAsync<SessionDataRequestDto>(request.Content!, "get_session_data", IsValidSessionDataRequest))
            .ReturnsResponse(TestFileTools.GetFile("GetSessionData.json"));
        handler.SetupRequest(HttpMethod.Post, "https://enviroservices.brighton-hove.gov.uk/xas/",
            request => HasActionAsync<RuntimeOperationRequestDto>(request.Content!, "runtimeOperation", IsValidPostCodeSearchRequest))
            .ReturnsResponse(TestFileTools.GetFile("PostCodeSearch.json"));
        handler.SetupRequest(HttpMethod.Post, "https://enviroservices.brighton-hove.gov.uk/xas/",
            request => HasActionAsync<RuntimeOperationRequestDto>(request.Content!, "runtimeOperation", IsValidAddressSelectionRequest))
            .ReturnsResponse(TestFileTools.GetFile("AddressSelection.json"));

        handler.SetupRequest(HttpMethod.Get, "https://enviroservices.brighton-hove.gov.uk/pages/en_GB/BartecCollective/Jobs_Get_Combined.page.xml").ReturnsResponse(TestFileTools.GetFile("JobsGetCombined.page.xml"));

        IMendixClient client = new MendixClient(httpClient);
        var res = await client.GetSchedule("BN1 8NT", 22058876);

        res.Should().NotBeNull();

        res.Where(a => a.Attributes["Collection_Date"].Value == "13/02/2026, 07:00").Should().NotBeEmpty();

        handler.VerifyAll();
    }

    private static bool IsValidSessionDataRequest(SessionDataRequestDto dto) => true;

    private static bool IsValidPostCodeSearchRequest(RuntimeOperationRequestDto dto) =>
        dto.OperationId.StartsWith("tglPIXhc")
        && GetKeyValue(dto.Changes, BHCCThemeAddress)["SearchString"].Value == "BN1 8NT"
        && dto.Params["Address"]["guid"].StartsWith(BHCCThemeAddress)
        && dto.Params.Count == 1
        && HasKeys(dto.Changes, new[] { BHCCThemeAddress, CollectionsCollection })
        && dto.Changes.Count == 2
        && HasGuids(dto.Objects, new[] { BHCCThemeAddress, CollectionsCollection })
        && dto.Objects.Length == 2;

    private static bool IsValidAddressSelectionRequest(RuntimeOperationRequestDto dto) =>
        dto.OperationId.StartsWith("DExhrgP")
        && dto.Params["Collection"]["guid"].StartsWith(CollectionsCollection)
        && dto.Params.Count == 1
        && HasKeys(dto.Changes, new[] { BHCCThemeAddress, CollectionsCollection, BHCCThemeAddressTempTable })
        && dto.Changes.Count == 44
        && HasGuids(dto.Objects, new[] { BHCCThemeAddress, CollectionsCollection, BHCCThemeAddressTempTable })
        && dto.Objects.Length == 44;

    private static bool HasGuids(ObjectDto[] objects, string[] guidStarts)
    {
        var keys = objects.Select(a => a.Guid);
        return guidStarts.All(k => keys.Any(x => x.StartsWith(k)));
    }

    private static bool HasKeys(IReadOnlyDictionary<long, Dictionary<string, HashValue>> dict, IReadOnlyList<string> keyStarts)
    {
        var keys = dict.Keys.Select(a => a.ToString());
        return keyStarts.All(k => keys.Any(x => x.StartsWith(k)));
    }

    private static IReadOnlyDictionary<string, HashValue> GetKeyValue(IReadOnlyDictionary<long, Dictionary<string, HashValue>> dict, string keyStart)
    {
        var key = dict.Keys.Single(k => k.ToString().StartsWith(keyStart));
        return dict[key];
    }

    private static async Task<bool> HasActionAsync<T>(HttpContent content, string expectedAction, Func<T, bool> pred) where T : RequestDtoBase
    {
        var json = (JsonContent)content;
        var dto = (RequestDtoBase)json.Value!;

        return dto.Action == expectedAction && pred((T)dto);
    }
}