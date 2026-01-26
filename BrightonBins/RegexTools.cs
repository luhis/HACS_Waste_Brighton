using System.Text.RegularExpressions;

namespace BrightonBins;

public static class RegexTools
{
    public static string GetPostCodeOperationId(string s)
    {
        var r = new Regex("\"operationId\":\"([A-Za-z0-9+/=]+)\",\"progress\":\\{\"message\":\"Looking up the address");
        var x = r.Match(s).Groups.Values;
        return x.ElementAt(1).Value;
    }
    public static string GetScheduleOperationId(string s)
    {
        var r = new Regex("\"operationId\":\"([A-Za-z0-9+/=]+)\",\"progress\":\\{\"message\":\"Finding your next collections");
        var x = r.Match(s).Groups.Values;
        return x.ElementAt(1).Value;
    }
}
