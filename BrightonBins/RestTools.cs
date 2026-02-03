using System.Net.Http.Json;
using System.Text.Json;

namespace BrightonBins;

public static class RestTools
{
    private static readonly JsonSerializerOptions serialiserSettings = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new Converter.AutoNumberToStringConverter()
        }
    };
    public static async Task<TR> PostAsJsonTypedAsync<T, TR>(this HttpClient httpClient, string url, T data)
    {
        var postcodeLookUpResonse = await httpClient.PostAsJsonAsync(url, data, serialiserSettings);

        if (postcodeLookUpResonse.IsSuccessStatusCode == false)
        {
            var s = JsonSerializer.Serialize(data, serialiserSettings);
            var error = await postcodeLookUpResonse.Content.ReadAsStringAsync();
            Console.WriteLine($"{error} {s}");
        }

        postcodeLookUpResonse.EnsureSuccessStatusCode();
        return await postcodeLookUpResonse.Content.ReadFromJsonAsync<TR>(serialiserSettings);
    }
}