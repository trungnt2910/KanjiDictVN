using Generator.HanViet;
using Generator.Models;
using HtmlAgilityPack;
using Knapcode.TorSharp;
using System.Net;
using System.Threading.Tasks;

namespace Generator.Converters;

public class HanNomConverter : ConverterBase
{
    private readonly string _cacheDir;
    private readonly int _fetcherMaxConcurrentJobs = 8;
    private readonly int _fetcherMaxRetries = 4;
    private readonly string _userAgent = "KanjiDictVN (https://github.com/trungnt2910/KanjiDictVN)";

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

        if (!Directory.Exists(_cacheDir))
        {
            Directory.CreateDirectory(_cacheDir);
        }

        // Stats
        var totalKanji = sourceKanjiBanks.Select(s => s.Length).Sum();
        var processedKanji = 0;
        var startTime = DateTime.Now;
        var torIdentityResetCount = 0;
        var waitingFetchers = 0;
        var downloadingFetchers = 0;

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);

        // Setup TorSharp
        Console.Error.WriteLine("[Tor] Setting up TorSharp...");
        var torSharpSettings = new TorSharpSettings
        {
            PrivoxySettings = { Disable = true }
        };
        var torSharpFetcher = new TorSharpToolFetcher(torSharpSettings, client);
        await torSharpFetcher.FetchAsync();
        using var torSharpProxy = new TorSharpProxy(torSharpSettings);
        await torSharpProxy.ConfigureAndStartAsync();
        using var torClientHandler = new HttpClientHandler()
        {
            Proxy = new WebProxy(new Uri("socks5://localhost:" + torSharpSettings.TorSettings.SocksPort))
        };
        using var torHttpClient = new HttpClient(torClientHandler);
        torHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(_userAgent);
       
        using var torManualResetEvent = new ManualResetEventSlim(true);
        using var torIdentityResetSemaphore = new SemaphoreSlim(1);
        var torIdentityResetCts = new CancellationTokenSource();
        var torCurrentIp = await torHttpClient.GetStringAsync("https://ipinfo.io/ip");
        Console.Error.WriteLine($"[Tor] TorSharp environment with ip <{torCurrentIp}> ready.");

        // Setup statistics thread
        using var statisticsThreadCts = new CancellationTokenSource();
        var statisticsThreadTask = Task.Run(async () =>
        {
            while (true)
            {
                if (statisticsThreadCts.Token.IsCancellationRequested)
                {
                    break;
                }

                if (processedKanji == 0)
                {
                    continue;
                }

                var periodTotalTime = DateTime.Now - startTime;
                Console.Error.WriteLine($"[Stats] Processed {processedKanji}/{totalKanji} kanji in {periodTotalTime}.");

                var averageSpeed = processedKanji / (double)periodTotalTime.TotalMinutes;
                Console.Error.WriteLine($"[Stats] Average speed: {averageSpeed} kanji/minute");

                Console.Error.WriteLine($"[Stats] Current IP: <{torCurrentIp}>.");
                Console.Error.WriteLine($"[Stats] {torIdentityResetCount} tor identity change(s) occurred.");

#if DEBUG
                Console.Error.WriteLine($"[Debug] {waitingFetchers} waiting for identity reset to complete.");
                Console.Error.WriteLine($"[Debug] {downloadingFetchers} waiting for download to complete.");
#endif

                var kanjiLeft = totalKanji - processedKanji;
                var estimatedTimeLeftMinutes = kanjiLeft / averageSpeed;
                Console.Error.WriteLine($"[Stats] ETA: {DateTime.Now.AddMinutes(estimatedTimeLeftMinutes)}");

                await Task.Delay(TimeSpan.FromSeconds(10), statisticsThreadCts.Token);
            }

        }, statisticsThreadCts.Token);

        foreach (var sourceKanjiBank in sourceKanjiBanks)
        {
            var resultKanjiBank = new DictionaryKanjiEntry[sourceKanjiBank.Length];

            var kanjiPerChunk = sourceKanjiBank.Length / _fetcherMaxConcurrentJobs;
            var fetcherTasks = new Task[_fetcherMaxConcurrentJobs];

            for (int i = 0; i < _fetcherMaxConcurrentJobs; ++i) 
            {
                fetcherTasks[i] = FetcherMain(i * kanjiPerChunk, 
                    (i == _fetcherMaxConcurrentJobs - 1) ? 
                        sourceKanjiBank.Length : 
                        (i + 1) * kanjiPerChunk);
            }

            async Task FetcherMain(int begin, int end)
            {
                for (int kanjiIndex = begin; kanjiIndex < end; ++kanjiIndex)
                {
                    var sourceKanjiEntry = sourceKanjiBank[kanjiIndex];
                    var kanji = sourceKanjiEntry.Character;
                    var escapedKanji = Uri.EscapeDataString(kanji);

                    var document = new HtmlDocument();

                    var cacheFile = Path.Combine(_cacheDir, escapedKanji);
                    if (File.Exists(cacheFile))
                    {
                        document.LoadHtml(await File.ReadAllTextAsync(cacheFile));
                    }
                    else
                    {
                        var hanVietUrl = $"https://hvdic.thivien.net/whv/{escapedKanji}";

                        for (int attempt = 0; attempt < _fetcherMaxRetries; ++attempt)
                        {
                            Interlocked.Increment(ref waitingFetchers);
                            torManualResetEvent.Wait();
                            Interlocked.Decrement(ref waitingFetchers);
                            var currentTorIdentityId = torIdentityResetCount;
                            var exceptionOccurred = false;

                            Interlocked.Increment(ref downloadingFetchers);
                            try
                            {
                                document.LoadHtml(await torHttpClient.GetStringAsync(hanVietUrl, torIdentityResetCts.Token));
                            }
                            catch (Exception e)
                            {
                                switch (e)
                                {
                                    case TaskCanceledException _:
                                    case IOException io when io.Message == "The response ended prematurely.":
                                        // Don't count this as a failed attempt.
                                        --attempt;
                                    break;
                                    default:
                                        Console.Error.WriteLine($"[{kanji}] [Fetch attempt {attempt + 1}] Exception: {e}");
                                    break;
                                }
                                exceptionOccurred = true;
                            }
                            Interlocked.Decrement(ref downloadingFetchers);

                            if (HvresDocument.IsValidDocument(document.DocumentNode))
                            {
                                break;
                            }
                            else if (!exceptionOccurred)
                            {
                                // Http fetch success, but invalid document. Probably because of a temporary IP block.

                                // Prevent other threads from reaching this block...
                                Interlocked.Increment(ref waitingFetchers);
                                torIdentityResetSemaphore.Wait();
                                Interlocked.Decrement(ref waitingFetchers);

                                // If it's not, the identity has already been reset.
                                if (currentTorIdentityId == torIdentityResetCount)
                                {
                                    // Prevent other threads from spamming requests while the identity is being reset...
                                    torManualResetEvent.Reset();
                                    ++torIdentityResetCount;

                                    // Forces all communication on the old client to stop
                                    torIdentityResetCts.Cancel();
                                    
                                    if (!torIdentityResetCts.TryReset())
                                    {
                                        torIdentityResetCts.Dispose();
                                        torIdentityResetCts = new();
                                    }

                                    Console.Error.WriteLine("[Tor] Old IP tempbanned, getting new IP...");
                                    await torSharpProxy.GetNewIdentityAsync();
                                    torCurrentIp = await torHttpClient.GetStringAsync("https://ipinfo.io/ip");
                                    Console.Error.WriteLine($"[Tor] New IP <{torCurrentIp}> is ready.");

                                    torManualResetEvent.Set();
                                }

                                torIdentityResetSemaphore.Release();
                            }
                        }

                        if (!HvresDocument.IsValidDocument(document.DocumentNode))
                        {
                            throw new InvalidOperationException($"[{kanji}] Cannot fetch kanji info.");
                        }

                        // Save the valid html for reuse and/or debugging purposes.
                        await File.WriteAllTextAsync(cacheFile, document.DocumentNode.OuterHtml);
                    }

                    // Strip every goddamn thing except the kanji.
                    var resultKanjiEntry = new DictionaryKanjiEntry()
                    {
                        Character = kanji
                    };

                    var hvresDocument = new HvresDocument(document.DocumentNode);

                    var overview = hvresDocument.Word.Details.Overview as HvresMeaningDictionary;

                    if (overview != null)
                    {
                        var dict = overview.Values;

                        if (dict.TryGetValue(HanVietProperty.HanVietReading, out var hanVietReading))
                        {
                            resultKanjiEntry.OnyomiReading.AddRange(
                                hanVietReading.Split(",").Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)));
                        }

                        if (dict.TryGetValue(HanVietProperty.Strokes, out var strokes))
                        {
                            resultKanjiEntry.Stats.Add(nameof(HanVietProperty.Strokes), strokes);
                        }

                        if (dict.TryGetValue(HanVietProperty.Radical, out var radical))
                        {
                            resultKanjiEntry.Stats.Add(nameof(HanVietProperty.Radical), radical);
                        }

                        if (dict.TryGetValue(HanVietProperty.PenStrokes, out var penStrokes))
                        {
                            resultKanjiEntry.Stats.Add(nameof(HanVietProperty.PenStrokes), penStrokes);
                        }

                        if (dict.TryGetValue(HanVietProperty.Shape, out var shape))
                        {
                            resultKanjiEntry.Stats.Add(nameof(HanVietProperty.Shape), shape);
                        }

                        if (dict.TryGetValue(HanVietProperty.Unicode, out var unicode))
                        {
                            resultKanjiEntry.Stats.Add(nameof(HanVietProperty.Unicode), unicode);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debugger.Break();
                    }

                    foreach (var entry in hvresDocument.Entries)
                    {
                        var spelling = entry.Header.Definition?.Spell?.Value;

                        if (spelling == null)
                        {
                            continue;
                        }

                        // We only extract data from the general dictionary. Other dictionaries provide definitions
                        // that are too complicated.
                        foreach (var meaning in entry.Details.Meanings.Where(kvp => kvp.Key.Name == "Từ điển phổ thông").Select(kvp => kvp.Value))
                        {
                            if (meaning is HvresMeaningList meaningList)
                            {
                                resultKanjiEntry.Meanings.AddRange(meaningList.Values.Select(s => $"[{spelling}] {s}"));
                            }
                        }
                    }

                    resultKanjiBank[kanjiIndex] = resultKanjiEntry;
                    Console.WriteLine($"Added: {resultKanjiEntry.Character}, {string.Join(", ", resultKanjiEntry.OnyomiReading)}");
                    Interlocked.Increment(ref processedKanji);
                }
            }

            await Task.WhenAll(fetcherTasks);

            resultKanjiBanks.Add(resultKanjiBank);
        }

        statisticsThreadCts.Cancel();
        torIdentityResetCts.Dispose();

        try
        {
            await statisticsThreadTask;
        }
        catch
        {
            // Ignore, this task does not have much value in processing data.
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
