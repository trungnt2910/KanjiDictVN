using Generator.Models;
using HtmlAgilityPack;
using System.Net;

namespace Generator.Converters;

public class HanNomConverter : ConverterBase
{
    private readonly string _cacheDir;
    private readonly int _archiveMaxRetries = 3;
    private readonly int _fetcherMaxConcurrentJobs = 8;
    private readonly int _archiveQueueMaxSize = 5;

    public HanNomConverter(string inputDir, string? cacheDir = null) : base(inputDir)
    {
        _cacheDir = cacheDir ?? ".cache";
    }

    protected override Task<DictionaryIndex> ConvertIndex(DictionaryIndex sourceIndex)
    {
        return Task.FromResult(new DictionaryIndex()
        {
            Title = "Từ điển Hán Nôm",
            Format = sourceIndex.Format,
            Revision = "trungnt2910.hannom." + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "." + sourceIndex.Revision,
            Sequenced = sourceIndex.Sequenced
        });
    }

    protected override async Task<IList<DictionaryKanjiEntry[]>> ConvertKanjiBanks(IList<DictionaryKanjiEntry[]> sourceKanjiBanks)
    {
        var resultKanjiBanks = new List<DictionaryKanjiEntry[]>();
        var totalKanji = sourceKanjiBanks.Select(s => s.Length).Sum();
        var processedKanji = 0;
        var totalArchiveCount = 0;
        var archivingQueueCount = 0;
        var startTime = DateTime.Now;

        using var client = new HttpClient();
        using var archiverClient = new WebArchiverClient(client);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("KanjiDictVN (https://github.com/trungnt2910/KanjiDictVN)");

        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }

        foreach (var sourceKanjiBank in sourceKanjiBanks)
        {
            var resultKanjiBank = new DictionaryKanjiEntry[sourceKanjiBank.Length];

            // TODO: Refactor this mess. I rushed through this stuff.

            var tasks = new List<Task>();
            var semaphore = new SemaphoreSlim(_archiveQueueMaxSize);

            for (int sourceKanjiEntryIndex = 0; sourceKanjiEntryIndex < sourceKanjiBank.Length; ++sourceKanjiEntryIndex)
            {
                var sourceKanjiEntry = sourceKanjiBank[sourceKanjiEntryIndex]!;
                var resultKanjiEntryIndex = sourceKanjiEntryIndex;
                
                // Archiving jobs don't count towards the limit.
                while (tasks.Count - archivingQueueCount >= _fetcherMaxConcurrentJobs)
                {
                    var timeoutTask = Task.Delay(500);
                    var completedTask = await Task.WhenAny(tasks.Concat(new[] { timeoutTask }));

                    if (completedTask == timeoutTask)
                    {
                        continue;
                    }

                    tasks.Remove(completedTask);
                    await completedTask;
                }

                tasks.Add(DoWork());

                async Task DoWork()
                {
                    var kanji = sourceKanjiEntry.Character;
                    var escapedKanji = Uri.EscapeDataString(kanji);

                    Console.Error.WriteLine($"[{processedKanji}/{totalKanji}] Querying Han-Nom details for: {kanji}...");

                    var document = new HtmlDocument();

                    var cacheFile = Path.Combine(_cacheDir, escapedKanji);
                    if (File.Exists(cacheFile))
                    {
                        document.LoadHtml(await File.ReadAllTextAsync(cacheFile));
                    }
                    else
                    {
                        var hanVietUrl = $"https://hvdic.thivien.net/whv/{escapedKanji}";
                        var hasGoodEntry = false;

                        for (int i = 0; i < _archiveMaxRetries; ++i)
                        {
                            var entries = await archiverClient.GetAvailableValidWebAsync(hanVietUrl).Reverse().ToListAsync();

                            foreach (var entry in entries)
                            {
                                var year = 0;
                                int.TryParse(entry.Timestamp.Substring(0, 4), out year);

                                // See: http://web.archive.org/web/20170217083531/http://hvdic.thivien.net/whv/%E9%9F%88
                                // The Han-Nom website before 2018 does not give us the required information.
                                if (year < 2018)
                                {
                                    continue;
                                }

                                document.LoadHtml(await archiverClient.GetContentAsStringAsync(entry));

                                if (document.DocumentNode.Descendants()
                                    .Where(x => x.HasClass("hvres-details"))
                                    .Any())
                                {
                                    // The page contains the Han-Nom details, this should be the correct page.
                                    hasGoodEntry = true;
                                    break;
                                }

                                Console.WriteLine($"[{kanji}] Revision {entry.Timestamp} not valid, attempting to use an older version...");
                            }

                            // Here, we might have archived the page, but the index might be faulty.
                            // Use the brute force method to load the latest page.
                            if (!hasGoodEntry && i > 0)
                            {
                                var latestTimestamp =
                                    await archiverClient.GetLatestTimestampByAlternativeMethodAsync(hanVietUrl);

                                if (latestTimestamp != null)
                                {
                                    document.LoadHtml(await archiverClient.GetContentAsStringAsync(new WebArchiverEntry()
                                    {
                                        Timestamp = latestTimestamp,
                                        Original = hanVietUrl
                                    }));
                                }
                                else
                                {
                                    // 2069, really, really far into the future.
                                    var bruteForceResponse =
                                        await client.GetAsync($"https://web.archive.org/web/20690109112305id_/https://hvdic.thivien.net/whv/{escapedKanji}");

                                    if (bruteForceResponse.IsSuccessStatusCode)
                                    {
                                        document.LoadHtml(await bruteForceResponse.Content.ReadAsStringAsync());
                                    }
                                }

                                if (document.DocumentNode.Descendants()
                                    .Where(x => x.HasClass("hvres-details"))
                                    .Any())
                                {
                                    // The page contains the Han-Nom details, this should be the correct page.
                                    hasGoodEntry = true;
                                }
                            }

                            if (!hasGoodEntry)
                            {
                                Interlocked.Increment(ref archivingQueueCount);

                                for (int j = 0; j < _archiveMaxRetries; ++j)
                                {
                                    if (j == 0)
                                    {
                                        Console.WriteLine($"[{kanji}] No valid revision found, requesting a fresh archive.");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"[{kanji}] Re-sending a request for a fresh archive.");
                                    }

                                    semaphore.Wait();
                                    var didArchive = await archiverClient.ArchiveAsync(hanVietUrl);
                                    semaphore.Release();

                                    if (didArchive)
                                    {
                                        Console.WriteLine($"[{kanji}] Archive successfully created.");
                                        Interlocked.Increment(ref totalArchiveCount);
                                        break;
                                    }
                                }

                                Interlocked.Decrement(ref archivingQueueCount);
                            }

                            if (hasGoodEntry)
                            {
                                break;
                            }
                        }

                        if (!hasGoodEntry)
                        {
                            // Still no good entry, last resort:
                            Console.WriteLine($"[{kanji}] Cannot fetch data from archive, fetching directly from Han-Nom website...");
                            document.LoadHtml(await client.GetStringAsync(hanVietUrl));

                            if (!document.DocumentNode.Descendants()
                                .Where(x => x.HasClass("hvres-details"))
                                .Any())
                            {
                                // Panic.
                                System.Diagnostics.Debugger.Break();
                                throw new InvalidOperationException($"Cannot get information for {kanji}");
                            }
                        }

                        // Save the valid html for reuse and/or debugging purposes.
                        await File.WriteAllTextAsync(cacheFile, document.DocumentNode.OuterHtml);
                    }

                    var details = document.DocumentNode.Descendants()
                        .Where(x => x.HasClass("hvres-details"));

                    var mainDetail = details.First();
                    var definitions = details.Skip(1).Where(d => d != null).ToArray();

                    // Strip every goddamn thing except the kanji.
                    var resultKanjiEntry = new DictionaryKanjiEntry()
                    {
                        Character = kanji
                    };

                    if (mainDetail != null)
                    {
                        var mainDetailFirstMeaning = mainDetail.Descendants()
                            .Where(d => d.HasClass("hvres-meaning"))
                            .FirstOrDefault();

                        var mainDetailFirstText = mainDetailFirstMeaning?.GetPlainText().Split(Environment.NewLine) ?? Array.Empty<string>();

                        var mainDetailFirstDictionary = mainDetailFirstText
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToDictionary(s => s.Substring(0, s.IndexOf(':')).Trim(), s => s.Substring(s.IndexOf(':') + 1).Trim());

                        if (mainDetailFirstDictionary.TryGetValue(HanVietProperty.HanVietReading, out var hanVietReading))
                        {
                            resultKanjiEntry.OnyomiReading.AddRange(
                                hanVietReading.Split(",").Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)));
                        }

                        if (mainDetailFirstDictionary.TryGetValue(HanVietProperty.Strokes, out var strokes))
                        {
                            resultKanjiEntry.Stats.Add(nameof(HanVietProperty.Strokes), strokes);
                        }

                        if (mainDetailFirstDictionary.TryGetValue(HanVietProperty.Radical, out var radical))
                        {
                            resultKanjiEntry.Stats.Add(nameof(HanVietProperty.Radical), radical);
                        }

                        if (mainDetailFirstDictionary.TryGetValue(HanVietProperty.PenStrokes, out var penStrokes))
                        {
                            resultKanjiEntry.Stats.Add(nameof(HanVietProperty.PenStrokes), penStrokes);
                        }

                        if (mainDetailFirstDictionary.TryGetValue(HanVietProperty.Shape, out var shape))
                        {
                            resultKanjiEntry.Stats.Add(nameof(HanVietProperty.Shape), shape);
                        }

                        if (mainDetailFirstDictionary.TryGetValue(HanVietProperty.Unicode, out var unicode))
                        {
                            resultKanjiEntry.Stats.Add(nameof(HanVietProperty.Unicode), unicode);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debugger.Break();
                    }

                    foreach (var definition in definitions)
                    {
                        var spelling = definition.ParentNode.Descendants()
                            .Where(d => d.HasClass("hvres-spell"))
                            .FirstOrDefault();

                        if (spelling == null)
                        {
                            continue;
                        }

                        var spellingText = spelling.GetPlainText().Trim();

                        // We only extract data from the general dictionary. Other dictionaries provide definitions
                        // that are too complicated.
                        var definitionTuDienPhoThongHeader = definition.Descendants()
                            .Where(d => d.HasClass("hvres-source") && d.InnerText.Trim() == "Từ điển phổ thông")
                            .FirstOrDefault();

                        if (definitionTuDienPhoThongHeader != null)
                        {
                            var definitionTuDienPhoThongContent = definitionTuDienPhoThongHeader.NextSibling;
                            while (definitionTuDienPhoThongContent != null && !definitionTuDienPhoThongContent.HasClass("hvres-meaning"))
                            {
                                definitionTuDienPhoThongContent = definitionTuDienPhoThongContent.NextSibling;
                            }
                            var definitionTuDienPhoThongText = definitionTuDienPhoThongContent?.GetPlainText().Split(Environment.NewLine)
                                .Select(s => s.Trim())
                                .Select(s => s.Trim("0123456789. ".ToCharArray()))
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Select(s => $"[{spellingText}] {s}")
                                .ToArray() ?? Array.Empty<string>();

                            resultKanjiEntry.Meanings.AddRange(definitionTuDienPhoThongText);
                        }
                    }

                    resultKanjiBank[resultKanjiEntryIndex] = resultKanjiEntry;
                    Console.WriteLine($"Added: {resultKanjiEntry.Character}, {string.Join(", ", resultKanjiEntry.OnyomiReading)}");

                    if (Interlocked.Increment(ref processedKanji) % 10 == 0)
                    {
                        var periodTotalTime = DateTime.Now - startTime;
                        Console.WriteLine($"Processed {processedKanji} kanji in {periodTotalTime}.");

                        Console.WriteLine($"Archives created: {totalArchiveCount} archive(s).");
                        Console.WriteLine($"Current archiving queue: {archivingQueueCount} jobs(s).");
                        Console.WriteLine($"Current tasks queue: {tasks.Count} jobs(s).");

                        var averageSpeed = processedKanji / (double)periodTotalTime.TotalMinutes;
                        Console.WriteLine($"Average speed: {averageSpeed} kanji/minute");

                        var kanjiLeft = totalKanji - processedKanji;
                        var estimatedTimeLeftMinutes = kanjiLeft / averageSpeed;
                        Console.WriteLine($"ETA: {DateTime.Now.AddMinutes(estimatedTimeLeftMinutes)}");
                    }
                };
            }

            await Task.WhenAll(tasks);

            resultKanjiBanks.Add(resultKanjiBank);
        }

        return resultKanjiBanks;
    }

    protected override Task<IList<DictionaryTagEntry[]>> ConvertTagBanks(IList<DictionaryTagEntry[]> sourceTagBanks)
    {
        return Task.FromResult<IList<DictionaryTagEntry[]>>(new List<DictionaryTagEntry[]>()
        {
            new DictionaryTagEntry[]
            {
                new DictionaryTagEntry()
                {
                    Name = nameof(HanVietProperty.Strokes),
                    Category = DictionaryTagCategory.Misc,
                    Notes = HanVietProperty.Strokes
                },
                new DictionaryTagEntry()
                {
                    Name = nameof(HanVietProperty.Radical),
                    Category = DictionaryTagCategory.Misc,
                    Notes = HanVietProperty.Radical
                },
                new DictionaryTagEntry()
                {
                    Name = nameof(HanVietProperty.PenStrokes),
                    Category = DictionaryTagCategory.Misc,
                    Notes = HanVietProperty.PenStrokes
                },
                new DictionaryTagEntry()
                {
                    Name = nameof(HanVietProperty.Shape),
                    Category = DictionaryTagCategory.Misc,
                    Notes = HanVietProperty.Shape
                },
                new DictionaryTagEntry()
                {
                    Name = nameof(HanVietProperty.Unicode),
                    Category = DictionaryTagCategory.Code,
                    Notes = HanVietProperty.Unicode
                },
            }
        });
    }
}
