using BrightonBins.Client;
using BrightonBins.Dtos;
using FluentAssertions;
using Moq;
using Moq.Contrib.HttpClient;
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
        dto.OperationId == "tglPIXhcJ1abaQ1fhKJvYA"
        && (string)ComparisonTools.GetKeyValue(dto.Changes, BHCCMendixConstants.BHCCThemeAddress)["SearchString"].Value == "BN1 8NT"
        && dto.Params["Address"]["guid"].StartsWith(BHCCMendixConstants.BHCCThemeAddress)
        && dto.Params.Count == 1
        && ComparisonTools.HasKeys(dto.Changes, new[] { BHCCMendixConstants.BHCCThemeAddress, BHCCMendixConstants.CollectionsCollection })
        && dto.Changes.Count == 2
        && ComparisonTools.HasGuids(dto.Objects, new[] { BHCCMendixConstants.BHCCThemeAddress, BHCCMendixConstants.CollectionsCollection })
        && dto.Objects.Length == 2;

    private static bool IsValidAddressSelectionRequest(RuntimeOperationRequestDto dto) =>
        dto.OperationId == "DExhrgP93VCBOks+s8GjTQ"
        && dto.Params["Collection"]["guid"].StartsWith(BHCCMendixConstants.CollectionsCollection)
        && dto.Params.Count == 1
        && ComparisonTools.HasKeys(dto.Changes, new[] { BHCCMendixConstants.BHCCThemeAddress, BHCCMendixConstants.CollectionsCollection, BHCCMendixConstants.BHCCThemeAddressTempTable })
        && (string)ComparisonTools.GetKeyValue(dto.Changes, BHCCMendixConstants.BHCCThemeAddress)["SearchString"].Value == "BN1 8NT"
        && ((string)ComparisonTools.GetKeyValue(dto.Changes, BHCCMendixConstants.BHCCThemeAddress)["Collections.Collection_Address"].Value).StartsWith(BHCCMendixConstants.CollectionsCollection)
        && ((string)ComparisonTools.GetKeyValue(dto.Changes, BHCCMendixConstants.BHCCThemeAddress)["BHCCTheme.AddressTemp_SelectedAddress"].Value).StartsWith(BHCCMendixConstants.UprnChangeElement)
        && (bool)ComparisonTools.GetKeyValue(dto.Changes, BHCCMendixConstants.CollectionsCollection)["DisplayCollectionsButton"].Value == true
        ////&& ComparisonTools.GetKeyValue(dto.Changes, BHCCMendixConstants.UprnChangeElement)["BHCCTheme.AddressTemp_SelectedAddress"].Value.StartsWith(BHCCMendixConstants.BHCCThemeAddress)
        && dto.Changes.Count == 44
        && ComparisonTools.HasGuids(dto.Objects, new[] { BHCCMendixConstants.BHCCThemeAddress, BHCCMendixConstants.CollectionsCollection, BHCCMendixConstants.BHCCThemeAddressTempTable })
        && dto.Objects.Length == 44;

    private static async Task<bool> HasActionAsync<T>(HttpContent content, string expectedAction, Func<T, bool> pred) where T : RequestDtoBase
    {
        var json = (JsonContent)content;
        var dto = (RequestDtoBase)json.Value!;

        return dto.Action == expectedAction && pred((T)dto);
    }
}
