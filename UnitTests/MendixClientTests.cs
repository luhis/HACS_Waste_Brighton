using BrightonBins;
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
    public async Task GetSchedule()
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

    private static async Task<bool> HasActionAsync(HttpContent content, string expectedAction)
    {
        var json = (await content.ReadFromJsonAsync<RequestDtoBase>(RestTools.serialiserSettings))!;
        return json.Action == expectedAction;
    }
}