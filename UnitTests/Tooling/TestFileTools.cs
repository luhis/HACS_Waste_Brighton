using BrightonBins.Dtos;

namespace UnitTests.Tooling;

public static class TestFileTools
{
    public static string GetFile(string filename) => System.IO.File.ReadAllText($"../../../JsonResponses/{filename}");
}


public static class ComparisonTools
{
    public static bool HasKeys(IReadOnlyDictionary<long, Dictionary<string, HashValue>> dict, IReadOnlyList<string> keyStarts)
    {
        var keys = dict.Keys.Select(a => a.ToString());
        return keyStarts.All(k => keys.Any(x => x.StartsWith(k)));
    }

    public static IReadOnlyDictionary<string, HashValue> GetKeyValue(IReadOnlyDictionary<long, Dictionary<string, HashValue>> dict, string keyStart)
    {
        var key = dict.Keys.Single(k => k.ToString().StartsWith(keyStart));
        return dict[key];
    }
    public static bool HasGuids(ObjectDto[] objects, string[] guidStarts)
    {
        var keys = objects.Select(a => a.Guid);
        return guidStarts.All(k => keys.Any(x => x.StartsWith(k)));
    }
}