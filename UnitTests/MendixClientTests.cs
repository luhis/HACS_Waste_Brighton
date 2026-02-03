using BrightonBins.Client;
using FluentAssertions;
using Moq;
using Moq.Contrib.HttpClient;

namespace UnitTests;

public class MendixClientTests
{
    [Fact]
    public async Task GetSchedule()
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = handler.CreateClient();

        IMendixClient client = new MendixClient(httpClient);
        var res = await client.GetSchedule("AA1 1AA", 12345678);

        res.Should().NotBeNull();

        handler.VerifyAll();
    }
}