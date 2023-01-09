using Generator.Models;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Generator;

public class WebArchiverClient: IDisposable
{
    private const string _baseApi = "https://web.archive.org/";
    private const int _archiveToleranceMinutes = 5;
    private const int _archiveMaxWaitSeconds = 200;

    private readonly HttpClient _client;
    private bool _disposeHttpClient = true;

    public WebArchiverClient(HttpClient client, bool disposeHttpClient = false)
    {
        _client = client;
        _disposeHttpClient = disposeHttpClient;
    }

    public WebArchiverClient()
        : this(new HttpClient(), true)
    {
        
    }

    /// <summary>
    /// Gets all available entries for <paramref name="url"/>.
    /// </summary>
    /// <returns>All available entries.</returns>
    public async IAsyncEnumerable<WebArchiverEntry> GetAvailableAsync(string url)
    {
        var response = await _client.GetStringAsync($"{_baseApi}cdx/search/cdx?url={url}");

        foreach (var entry in response.Split("\n", 
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return new WebArchiverEntry(entry);
        }
    }

    /// <summary>
    /// Gets all available valid web page captures for <paramref name="url"/>.
    /// A "valid web page capture" is an entry that returns a "text/html" content and a status code of 200.
    /// </summary>
    /// <returns>Valid web page entries.</returns>
    public async IAsyncEnumerable<WebArchiverEntry> GetAvailableValidWebAsync(string url)
    {
        await foreach (var entry in GetAvailableAsync(url))
        {
            if (entry.StatusCode == "200" && entry.MimeType == "text/html")
            {
                yield return entry;
            }
        }
    }

    public async Task<string> GetContentAsStringAsync(WebArchiverEntry entry)
    {
        return await _client.GetStringAsync($"{_baseApi}web/{entry.Timestamp}id_/{entry.Original}");
    }

    /// <summary>
    /// Gets the latest saved timestamp for <paramref name="url"/> without knowing the archiver entry.
    /// The saved page at this timestamp is not guaranteed to be valid.
    /// </summary>
    /// <param name="url">The desired URL.</param>
    /// <returns>The web archiver timestamp, or <see langword="null"/> if it cannot be determined.</returns>
    public async Task<string?> GetLatestTimestampByAlternativeMethodAsync(string url)
    {
        var currentDateAndOneHour = DateTime.Now.AddHours(1).ToString("yyyyMMddHHmmss");
        var response = await _client.GetAsync($"{_baseApi}web/{currentDateAndOneHour}id_/{url}");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var redirectedUrl = response.RequestMessage?.RequestUri?.ToString() ?? string.Empty;

        var match = Regex.Match(redirectedUrl, "http(?:s)*:\\/\\/web\\.archive\\.org\\/web\\/(\\d*)(?:id_)/.*", RegexOptions.Compiled);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    /// <summary>
    /// Requests an archive on web.archive.org for <paramref name="url" />.
    /// </summary>
    /// <param name="url">The URL of the page to archive.</param>
    /// <returns>True if the attempt succeeds.</returns>
    public async Task<bool> ArchiveAsync(string url)
    {
        var retrySeconds = 1;

        while (retrySeconds <= _archiveMaxWaitSeconds)
        {
            var saveApi = $"{_baseApi}save/{url}";
            HttpResponseMessage archiveResponse;

            try
            {
                archiveResponse = await _client.GetAsync(saveApi);
            }
            catch (Exception e)
            {
                // Don't know what to do here yet...
                // System.Diagnostics.Debugger.Break();
                break;
            }

            if (archiveResponse.Headers.Contains("X-Archive-Wayback-Runtime-Error"))
            {
                // Don't know what to do here yet...
                System.Diagnostics.Debugger.Break();
                break;
            }

            if (!archiveResponse.IsSuccessStatusCode)
            {
                if (archiveResponse.StatusCode == (HttpStatusCode)520)
                {
                    // While the response is 520 the site still gets archived.
                    var latestArchiveStamp = await GetLatestTimestampByAlternativeMethodAsync(url);

                    latestArchiveStamp ??= (await GetAvailableValidWebAsync(url).FirstOrDefaultAsync())?.Timestamp;

                    if (latestArchiveStamp == null)
                    {
                        return false;
                    }

                    if (DateTime.TryParseExact(latestArchiveStamp, 
                            "yyyyMMddHHmmss", 
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var timestamp))
                    {
                        if (DateTime.UtcNow - timestamp > TimeSpan.FromMinutes(_archiveToleranceMinutes))
                        {
                            return false;
                        }

                        // A recent copy is actually available.
                        return true;
                    }
                }
                else if (archiveResponse.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(TimeSpan.FromSeconds(retrySeconds));
                    retrySeconds *= 2;
                    continue;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _client.Dispose();
            _disposeHttpClient = false;
        }

        GC.SuppressFinalize(this);
    }
}
