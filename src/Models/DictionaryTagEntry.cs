namespace Generator.Models;

// See: https://github.com/FooSoft/yomichan/blob/master/ext/data/schemas/dictionary-tag-bank-v3-schema.json
public class DictionaryTagEntry
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Order { get; set; } = 0;
    public string Notes { get; set; } = string.Empty;
    public int Popularity { get; set; } = 0;

    public DictionaryTagEntry()
    {

    }

    public DictionaryTagEntry(object[] jsonRepresentation)
    {
        Name = jsonRepresentation[0].GetOrDeserialize<string>() ?? Name;
        Category = jsonRepresentation[1].GetOrDeserialize<string>() ?? Category;
        Order = jsonRepresentation[2].GetOrDeserialize<int>();
        Notes = jsonRepresentation[3].GetOrDeserialize<string>() ?? Notes;
        Popularity = jsonRepresentation[4].GetOrDeserialize<int>();
    }

    public object[] ToObjectRepresentation()
    {
        return new object[]
        {
            Name,
            Category,
            Order,
            Notes,
            Popularity
        };
    }
}
