using System.Text.RegularExpressions;

namespace BrightonBins;

public static class RegexTools
{
    public static string GetPostCodeOperationId(string s)
    {
        var r = new Regex("\"operationId\":\"(?<operationId>[A-Za-z0-9+/=]+)\",\"progress\":\\{\"message\":\"Looking up the address");
        return r.Match(s).Groups["operationId"].Value;
    }
    public static string GetScheduleOperationId(string s)
    {
        var r = new Regex("\"operationId\":\"(?<operationId>[A-Za-z0-9+/=]+)\",\"progress\":\\{\"message\":\"Finding your next collections");
        return r.Match(s).Groups["operationId"].Value;
    }
}
