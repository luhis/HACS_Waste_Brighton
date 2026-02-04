using BrightonBins;
using FluentAssertions;

namespace UnitTests;

public class RegexToolsTests
{
    private readonly string Input = TestFileTools.GetFile("JobsGetCombined.page.xml");

    [Fact]
    public void GetPostCodeOperationId()
    {
        var res = RegexTools.GetPostCodeOperationId(Input);

        res.Should().Be("tglPIXhcJ1abaQ1fhKJvYA");
    }
    [Fact]
    public void GetScheduleOperationId()
    {
        var res = RegexTools.GetScheduleOperationId(Input);

        res.Should().Be("DExhrgP93VCBOks+s8GjTQ");
    }
}
