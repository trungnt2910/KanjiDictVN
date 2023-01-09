using Generator.Models;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Generator.Converters;

public abstract class ConverterBase
{
    public DictionaryIndex SourceIndex { get; }

    public IList<DictionaryTagEntry[]> SourceTagBanks { get; }

    public IList<DictionaryKanjiEntry[]> SourceKanjiBanks { get; }

    public DictionaryIndex? ResultIndex { get; private set; }

    public IList<DictionaryTagEntry[]>? ResultTagBanks { get; private set; }

    public IList<DictionaryKanjiEntry[]>? ResultKanjiBanks { get; private set; }

    protected ConverterBase(DictionaryIndex index, IList<DictionaryTagEntry[]> tagBanks, IList<DictionaryKanjiEntry[]> kanjiBanks)
    {
        SourceIndex = index;
        SourceTagBanks = tagBanks;
        SourceKanjiBanks = kanjiBanks;
    }

    protected ConverterBase(string inputDir)
    {
        var index = JsonSerializer.Deserialize<DictionaryIndex>(
            File.ReadAllText(Path.Combine(inputDir, "index.json")));

        if (index == null)
        {
            throw new InvalidDataException($"Cannot read index.json from {inputDir}");
        }

        SourceIndex = index;
        
        // Tag and Kanji banks are 1-BASED INDEXED!

        SourceTagBanks = new List<DictionaryTagEntry[]>();
        for (int i = 1; ; ++i)
        {
            var bankPath = Path.Combine(inputDir, $"tag_bank_{i}.json");
            if (!Path.Exists(bankPath))
            {
                break;
            }

            var bankJson = JsonSerializer.Deserialize<object[][]>(File.ReadAllText(bankPath));
            if (bankJson == null)
            {
                throw new InvalidDataException($"Cannot read {bankPath}");
            }

            SourceTagBanks.Add(bankJson.Select(e => new DictionaryTagEntry(e)).ToArray());
        }

        SourceKanjiBanks = new List<DictionaryKanjiEntry[]>();
        for (int i = 1; ; ++i)
        {
            var bankPath = Path.Combine(inputDir, $"kanji_bank_{i}.json");
            if (!Path.Exists(bankPath))
            {
                break;
            }

            var bankJson = JsonSerializer.Deserialize<object[][]>(File.ReadAllText(bankPath));
            if (bankJson == null)
            {
                throw new InvalidDataException($"Cannot read {bankPath}");
            }

            SourceKanjiBanks.Add(bankJson.Select(e => new DictionaryKanjiEntry(e)).ToArray());
        }
    }

    public void Write(string outputDir)
    {
        var serializeOptions = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        File.WriteAllText(Path.Combine(outputDir, "index.json"),
            JsonSerializer.Serialize(ResultIndex, serializeOptions));

        for (int i = 1; i <= ResultTagBanks!.Count; ++i)
        {
            File.WriteAllText(Path.Combine(outputDir, $"tag_bank_{i}.json"),
                JsonSerializer.Serialize(ResultTagBanks[i - 1].Select(e => e.ToObjectRepresentation()).ToArray(), serializeOptions));
        }

        for (int i = 1; i <= ResultKanjiBanks!.Count; ++i)
        {
            File.WriteAllText(Path.Combine(outputDir, $"kanji_bank_{i}.json"),
                JsonSerializer.Serialize(ResultKanjiBanks[i - 1].Select(e => e.ToObjectRepresentation()).ToArray(), serializeOptions));
        }
    }

    public async Task Convert()
    {
        ResultIndex = await ConvertIndex(SourceIndex);
        ResultTagBanks = await ConvertTagBanks(SourceTagBanks);
        ResultKanjiBanks = await ConvertKanjiBanks(SourceKanjiBanks);
    }

    protected abstract Task<DictionaryIndex> ConvertIndex(DictionaryIndex sourceIndex);
    protected abstract Task<IList<DictionaryTagEntry[]>> ConvertTagBanks(IList<DictionaryTagEntry[]> sourceTagBanks);
    protected abstract Task<IList<DictionaryKanjiEntry[]>> ConvertKanjiBanks(IList<DictionaryKanjiEntry[]> sourceKanjiBanks);
}
