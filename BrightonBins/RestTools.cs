using System.Net.Http.Json;
using System.Text.Json;

namespace BrightonBins;

public static class RestTools
{
    public static readonly JsonSerializerOptions serialiserSettings = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        Converters =
        {
            new Converter.AutoNumberToStringConverter()
        }
    };
    public static async Task<TR> PostAsJsonTypedAsync<T, TR>(this HttpClient httpClient, string url, T data)
    {
        var response = await httpClient.PostAsJsonAsync(url, data, serialiserSettings);

        if (response.IsSuccessStatusCode == false)
        {
            var s = JsonSerializer.Serialize(data, serialiserSettings);
            var error = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"{error} {s}");
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TR>(serialiserSettings))!;
    }

    public static async Task<string> GetAsString(this HttpClient httpClient, string url)
    {
        var response = await httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}