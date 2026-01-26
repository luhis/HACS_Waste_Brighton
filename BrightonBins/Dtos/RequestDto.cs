using System.Collections.Generic;

namespace BrightonBins.Dtos;

public class RequestDto
{
    public string Action => "runtimeOperation";
    public Dictionary<string, Dictionary<string, string>> Params { get; init; }
    public Dictionary<long, Dictionary<string, HashValue>> Changes { get; init; }
    public ObjectDto[] Objects { get; init; }
    public string OperationId { get; init; }
}
