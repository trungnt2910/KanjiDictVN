using System.Text.Json.Serialization;

namespace Generator.Models;

public class DictionaryIndex
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public int Format { get; set; } = 3;

    [JsonPropertyName("revision")]
    public string Revision { get; set; } = string.Empty;

    [JsonPropertyName("sequenced")]
    public bool Sequenced { get; set; } = false;

    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("attribution")]
    public string Attribution { get; set; } = string.Empty;

    [JsonPropertyName("isUpdatable")]
    public bool IsUpdatable { get; set; } = false;

    [JsonPropertyName("indexUrl")]
    public string? IndexUrl { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string? DownloadUrl { get; set; }
}
