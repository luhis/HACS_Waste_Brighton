namespace BrightonBins.Dtos;

public class SessionDataRequestDto : RequestDtoBase
{
    public SessionDataRequestDto()
    {
        Action = "get_session_data";
    }
    public required Dictionary<string, object?> Params { get; init; }
}
