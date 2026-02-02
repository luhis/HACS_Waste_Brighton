using System.Net.Http.Json;
using System.Text.Json;

namespace BrightonBins;

public static class RestTools
{
    private static readonly System.Text.Json.JsonSerializerOptions serialiserSettings = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new Converter.AutoNumberToStringConverter()
        }
    };
    public static async Task<TR> PostAsJsonTypedAsync<T, TR>(this HttpClient httpClient, string url, T data)
    {
        var postcodeLookUpResonse = await httpClient.PostAsJsonAsync(url, data, serialiserSettings);

        postcodeLookUpResonse.EnsureSuccessStatusCode();
        return await postcodeLookUpResonse.Content.ReadFromJsonAsync<TR>(serialiserSettings);
    }
}