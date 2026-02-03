namespace BrightonBins.Dtos;

public class RequestDto
{
    public string Action => "runtimeOperation";
    public required Dictionary<string, Dictionary<string, string>> Params { get; init; }
    public required Dictionary<long, Dictionary<string, HashValue>> Changes { get; init; }
    public required ObjectDto[] Objects { get; init; }
    public required string OperationId { get; init; }
}
