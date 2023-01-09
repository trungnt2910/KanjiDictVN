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
}
