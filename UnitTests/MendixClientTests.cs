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

    [Fact]
    public async Task GetSchedule_Filtered()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = handler.CreateClient();

        handler.SetupRequest(HttpMethod.Get, "https://enviroservices.brighton-hove.gov.uk/link/collections")
            .ReturnsResponse(System.Net.HttpStatusCode.OK);

        handler.SetupRequest(HttpMethod.Post, "https://enviroservices.brighton-hove.gov.uk/xas/", 
            request => HasActionAsync<SessionDataRequestDto>(request.Content!, "get_session_data"))
            .ReturnsResponse(TestFileTools.GetFile("GetSessionData.json"));
        handler.SetupRequestSequence(HttpMethod.Post, "https://enviroservices.brighton-hove.gov.uk/xas/", 
            request => HasActionAsync<RuntimeOperationRequestDto>(request.Content!, "runtimeOperation"))
            .ReturnsResponse(TestFileTools.GetFile("PostCodeSearch.json"))
            .ReturnsResponse(TestFileTools.GetFile("AddressSelection.json"));

        handler.SetupRequest(HttpMethod.Get, "https://enviroservices.brighton-hove.gov.uk/pages/en_GB/BartecCollective/Jobs_Get_Combined.page.xml").ReturnsResponse(TestFileTools.GetFile("JobsGetCombined.page.xml"));

        IMendixClient client = new MendixClient(httpClient);
        var res = await client.GetSchedule("BN1 8NT", 22058876);

        res.Should().NotBeNull();

        res.Where(a => a.Attributes["Collection_Date"].Value == "13/02/2026, 07:00").Should().NotBeEmpty();

        handler.VerifyAll();
    }

    private static async Task<bool> HasActionAsync<T>(HttpContent content, string expectedAction) where T : RequestDtoBase
    {
        var json = (JsonContent)content;
        var dto = (RequestDtoBase)json.Value!;

        return dto.Action == expectedAction;
    }
}