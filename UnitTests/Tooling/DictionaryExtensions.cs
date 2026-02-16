using BrightonBins.Dtos;

namespace UnitTests.Tooling;

public static class DictionaryExtensions
{
    public static T Value<T>(this IReadOnlyDictionary<string, HashValue> dict, string key)
    {
        return (T)dict[key].Value;
    }
}
