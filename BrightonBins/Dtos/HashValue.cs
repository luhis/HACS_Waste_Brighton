namespace BrightonBins.Dtos;

public class HashValue
{
    [System.Text.Json.Serialization.JsonPropertyName("hash")]
    public string Hash { get; init; }
    [System.Text.Json.Serialization.JsonPropertyName("value")]
    public string Value { get; init; }
}