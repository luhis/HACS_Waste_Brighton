using BrightonBins;
using FluentAssertions;
using System.Threading.Tasks;

namespace UnitTests;

public class RegexToolsTests
{
    private readonly string Input = System.IO.File.ReadAllText("../../../JsonResponses/JobsGetCombined.page.xml");

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
