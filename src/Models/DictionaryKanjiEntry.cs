namespace Generator.Models;

// See: https://github.com/FooSoft/yomichan/blob/master/ext/data/schemas/dictionary-kanji-bank-v3-schema.json
public class DictionaryKanjiEntry
{
    public string Character { get; set; } = string.Empty;
    public List<string> OnyomiReading { get; set; } = new();
    public List<string> KunyomiReading { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<string> Meanings { get; set; } = new();
    public Dictionary<string, string> Stats { get; set; } = new();

    public DictionaryKanjiEntry()
    {
    }

    public DictionaryKanjiEntry(object[] jsonRepresentation)
    {
        Character = jsonRepresentation[0].GetOrDeserialize<string>() ?? Character;
        OnyomiReading = jsonRepresentation[1].GetOrDeserialize<string>()
                            ?.Split(" ").ToList() ?? OnyomiReading;
        KunyomiReading = jsonRepresentation[2].GetOrDeserialize<string>()
                            ?.Split(" ").ToList() ?? KunyomiReading;
        Tags = jsonRepresentation[3].GetOrDeserialize<string>()
                            ?.Split(" ").ToList() ?? Tags;
        Meanings = jsonRepresentation[4].GetOrDeserialize<string[]>()?.ToList() ?? Meanings;
        Stats = jsonRepresentation[5].GetOrDeserialize<Dictionary<string, string>>() ?? Stats;
    }

    public object[] ToObjectRepresentation()
    {
        return new object[]
        {
            Character,
            string.Join(" ", OnyomiReading),
            string.Join(" ", KunyomiReading),
            string.Join(" ", Tags),
            Meanings,
            Stats
        };
    }
}
