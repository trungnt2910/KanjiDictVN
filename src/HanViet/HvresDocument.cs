using HtmlAgilityPack;

namespace Generator.HanViet;

public class HvresDocument
{
    public HvresHanWord Word { get; set; }
    public List<HvresEntry> Entries { get; set; }

    public HvresDocument(HtmlNode node)
    {
        var bases = node.Descendants()
            .Where(n => n.HasClass("hvres") && (n.HasClass("han-word") || !string.IsNullOrEmpty(n.Attributes["name"]?.Value)))
            .ToList();

        Word = new HvresHanWord(bases[0]);
        Entries = bases.Skip(1).Select(b => new HvresEntry(b)).ToList();
    }

    public HvresDocument()
    {
        Word = new();
        Entries = new();
    }

    public static bool IsValidDocument(HtmlNode node)
    {
        // The page contains the Han-Nom details, this should be the correct page.
        return node.Descendants()
            .Where(x => x.HasClass("hvres-details"))
            .Any();
    }
    public static string TruncateHtml(HtmlNode node)
    {
        var bases = node.Descendants()
        .Where(n => n.HasClass("hvres") && (n.HasClass("han-word") || !string.IsNullOrEmpty(n.Attributes["name"]?.Value)))
        .ToList();
        using var writer = new StringWriter();
        foreach (var item in bases)
        {
            item.WriteTo(writer);
            writer.WriteLine();
        }
        return writer.ToString();
    }
}
