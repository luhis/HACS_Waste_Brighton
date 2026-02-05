namespace BrightonBins.Dtos;

public class RuntimeOperationRequestDto : RequestDtoBase
{
    public RuntimeOperationRequestDto()
    {
        Action = "runtimeOperation";
    }
    public required Dictionary<string, Dictionary<string, string>> Params { get; init; }
    public required Dictionary<long, Dictionary<string, HashValue>> Changes { get; init; }
    public required ObjectDto[] Objects { get; init; }
    public required string OperationId { get; init; }
}
