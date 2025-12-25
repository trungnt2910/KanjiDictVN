using Generator.Models;

namespace Generator.Converters;

public class DebloatConverter : ConverterBase
{
    public DebloatConverter(string inputDir) : base(inputDir)
    {
    }

    protected override Task<DictionaryIndex> ConvertIndex(DictionaryIndex sourceIndex)
    {
        return Task.FromResult(new DictionaryIndex()
        {
            Title = sourceIndex.Title + " - Debloated",
            Format = sourceIndex.Format,
            Revision = "trungnt2910." + sourceIndex.Revision + ".debloated",
            Sequenced = sourceIndex.Sequenced,
            Author = sourceIndex.Author,
            Url = Utilities.GetAssemblyRepositoryUrl(),
            Description = sourceIndex.Description,
            Attribution = sourceIndex.Attribution,
            IsUpdatable = true,
            IndexUrl = $"{Utilities.GetAssemblyRepositoryUrl()}/releases/latest/download/"
                + "KANJIDIC_english.json",
            DownloadUrl = $"{Utilities.GetAssemblyRepositoryUrl()}/release/latest/download/"
                + "KANJIDIC_english.zip"
        });
    }

    protected override Task<IList<DictionaryKanjiEntry[]>> ConvertKanjiBanks(IList<DictionaryKanjiEntry[]> sourceKanjiBanks)
    {
        var resultKanjiBanks = new List<DictionaryKanjiEntry[]>();

        foreach (var sourceKanjiBank in sourceKanjiBanks)
        {
            var resultKanjiBank = new List<DictionaryKanjiEntry>();

            foreach (var sourceKanjiEntry in sourceKanjiBank)
            {
                // Purge all index information.
                var newStats = new Dictionary<string, string>(sourceKanjiEntry.Stats);
                foreach (var kvp in sourceKanjiEntry.Stats)
                {
                    var category = SourceTagBanks.SelectMany(b => b).FirstOrDefault(e => e.Name == kvp.Key)?.Category;
                    if (category == DictionaryTagCategory.Index || category == DictionaryTagCategory.Class)
                    {
                        newStats.Remove(kvp.Key);
                    }
                }

                resultKanjiBank.Add(new DictionaryKanjiEntry()
                {
                    Character = sourceKanjiEntry.Character,
                    OnyomiReading = sourceKanjiEntry.OnyomiReading,
                    KunyomiReading = sourceKanjiEntry.KunyomiReading,
                    Tags = sourceKanjiEntry.Tags,
                    Meanings = sourceKanjiEntry.Meanings,
                    Stats = newStats
                });
            }

            resultKanjiBanks.Add(resultKanjiBank.ToArray());
        }

        return Task.FromResult<IList<DictionaryKanjiEntry[]>>(resultKanjiBanks);
    }

    protected override Task<IList<DictionaryTagEntry[]>> ConvertTagBanks(IList<DictionaryTagEntry[]> sourceTagBanks)
    {
        var resultTagBanks = new List<DictionaryTagEntry[]>();

        foreach (var sourceTagBank in sourceTagBanks)
        {
            var resultTagBank = sourceTagBank
                .Where(t => t.Category != DictionaryTagCategory.Index)
                .ToArray();

            resultTagBanks.Add(resultTagBank);
        }

        return Task.FromResult<IList<DictionaryTagEntry[]>>(resultTagBanks);
    }
}
