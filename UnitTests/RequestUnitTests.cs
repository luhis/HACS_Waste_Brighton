using BrightonBins;
using BrightonBins.Dtos;
using UnitTests.Tooling;

namespace UnitTests;

public class RequestUnitTests
{
    [Fact]
    public void CompareFromWebsiteAndFromAppJsonRequests()
    {
        // Arrange
        var fromWebsiteJson = File.ReadAllText("../../../JsonRequests/FromWebsite.json");
        var fromAppJson = File.ReadAllText("../../../JsonRequests/FromApp.json");

        var fromWebsite = System.Text.Json.JsonSerializer.Deserialize<RuntimeOperationRequestDto>(fromWebsiteJson, RestTools.serialiserSettings);
        var fromApp = System.Text.Json.JsonSerializer.Deserialize<RuntimeOperationRequestDto>(fromAppJson, RestTools.serialiserSettings);

        // Assert - Basic structure
        fromWebsite.Should().NotBeNull();
        fromApp.Should().NotBeNull();

        // Assert - Action should match
        fromWebsite!.Action.Should().Be(fromApp!.Action);
        fromWebsite.Action.Should().Be("runtimeOperation");

        // Assert - Operation IDs should start with the same prefix
        fromApp.OperationId.Should().StartWith(fromWebsite.OperationId);

        // Assert - Params structure
        fromWebsite.Params.Should().ContainKey("Collection");
        fromApp.Params.Should().ContainKey("Collection");
        fromWebsite.Params["Collection"]["guid"].Should().StartWith(BHCCMendixConstants.CollectionsCollection);
        fromApp.Params["Collection"]["guid"].Should().StartWith(BHCCMendixConstants.CollectionsCollection);

        // Assert - Changes should contain expected object types
        ComparisonTools.HasKeys(fromWebsite.Changes, new[] { BHCCMendixConstants.BHCCThemeAddress, BHCCMendixConstants.CollectionsCollection, BHCCMendixConstants.BHCCThemeAddressTempTable })
            .Should().BeTrue("FromWebsite should contain all expected keys");
        ComparisonTools.HasKeys(fromApp.Changes, new[] { BHCCMendixConstants.BHCCThemeAddress, BHCCMendixConstants.CollectionsCollection, BHCCMendixConstants.BHCCThemeAddressTempTable })
            .Should().BeTrue("FromApp should contain all expected keys");

        // Assert - Both should have 44 changes (1 Collection + 1 Address + 42 AddressTemp entries)
        fromWebsite.Changes.Count.Should().Be(44);
        fromApp.Changes.Count.Should().Be(44);

        fromWebsite.Changes.Where(a => a.Value.ContainsKey("Collections.Collection_Address")).Should()
            .HaveCount(fromApp.Changes.Where(a => a.Value.ContainsKey("Collections.Collection_Address")).Count());

        // Assert - Objects structure
        ComparisonTools.HasGuids(fromWebsite.Objects, new[] { BHCCMendixConstants.BHCCThemeAddress, BHCCMendixConstants.CollectionsCollection, BHCCMendixConstants.BHCCThemeAddressTempTable })
            .Should().BeTrue("FromWebsite should contain all expected object types");
        ComparisonTools.HasGuids(fromApp.Objects, new[] { BHCCMendixConstants.BHCCThemeAddress, BHCCMendixConstants.CollectionsCollection, BHCCMendixConstants.BHCCThemeAddressTempTable })
            .Should().BeTrue("FromApp should contain all expected object types");

        fromApp.Objects.Length.Should().Be(fromWebsite.Objects.Length);

        fromApp.Objects.GroupBy(a => a.ObjectType).Select(a => a.Key).Should().BeEquivalentTo(fromWebsite.Objects.GroupBy(a => a.ObjectType).Select(a => a.Key));

        // Assert - Collection object should have DisplayCollectionsButton set
        var fromWebsiteCollection = ComparisonTools.GetKeyValue(fromWebsite.Changes, BHCCMendixConstants.CollectionsCollection);
        var fromAppCollection = ComparisonTools.GetKeyValue(fromApp.Changes, BHCCMendixConstants.CollectionsCollection);

        fromWebsiteCollection.Should().ContainKey("DisplayCollectionsButton");
        fromAppCollection.Should().ContainKey("DisplayCollectionsButton");

        // Assert - Address object should have SearchString in FromWebsite
        var fromWebsiteAddress = ComparisonTools.GetKeyValue(fromWebsite.Changes, BHCCMendixConstants.BHCCThemeAddress);
        fromWebsiteAddress.Should().ContainKey("SearchString");
        fromWebsiteAddress["SearchString"].Value.Should().Be("BN1 8NT");

        // Assert - Both should have the same number of AddressTempTable entries
        var fromWebsiteTempAddresses = fromWebsite.Objects
            .Where(o => o.ObjectType == "BHCCTheme.AddressTempTable")
            .ToList();
        var fromAppTempAddresses = fromApp.Objects
            .Where(o => o.ObjectType == "BHCCTheme.AddressTempTable")
            .ToList();

        fromWebsiteTempAddresses.Should().HaveCount(42);
        fromAppTempAddresses.Should().HaveCount(42);

        // Assert - All temp addresses should have display values
        fromWebsiteTempAddresses.Should().AllSatisfy(addr =>
        {
            var changes = fromWebsite.Changes
                .Where(c => c.Key.ToString() == addr.Guid)
                .Select(c => c.Value)
                .FirstOrDefault();
            changes.Should().NotBeNull();
            changes!.Should().ContainKey("display");
        });

        fromAppTempAddresses.Should().AllSatisfy(addr =>
        {
            var changes = fromApp.Changes
                .Where(c => c.Key.ToString() == addr.Guid)
                .Select(c => c.Value)
                .FirstOrDefault();
            changes.Should().NotBeNull();
            changes!.Should().ContainKey("display");
        });
    }
}