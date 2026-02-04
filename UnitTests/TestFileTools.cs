namespace UnitTests;

public static class TestFileTools
{
    public static string GetFile(string filename) => System.IO.File.ReadAllText($"../../../JsonResponses/{filename}");
}
