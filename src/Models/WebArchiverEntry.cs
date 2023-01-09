namespace Generator.Models;

public class WebArchiverEntry
{
    public string UrlKey { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public string Original { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public string Digest { get; set; } = string.Empty;
    public string Length { get; set; } = string.Empty;

    public WebArchiverEntry(string cdxInfo)
    {
        var cdxInfoParts = cdxInfo.Split(" ", StringSplitOptions.TrimEntries);

        UrlKey = cdxInfoParts[0];
        Timestamp = cdxInfoParts[1];
        Original = cdxInfoParts[2];
        MimeType = cdxInfoParts[3];
        StatusCode = cdxInfoParts[4];
        Digest = cdxInfoParts[5];
        Length = cdxInfoParts[6];
    }

    public WebArchiverEntry() { }
}
