using HtmlAgilityPack;
using System.Net;

namespace Generator.HanViet;

public class HvresMeaningDictionary : HvresMeaning
{
    public Dictionary<string, string> Values { get; set; } = new();

    public HvresMeaningDictionary(HtmlNode node) : base(node)
    {
        var nodeText = node.GetPlainText();
        var nodeTextLines = nodeText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        Values = nodeTextLines
            .Select(l => WebUtility.HtmlDecode(l))
            .ToDictionary(s => s.Substring(0, s.IndexOf(':')).Trim(), s => s.Substring(s.IndexOf(':') + 1).Trim());
    }

    public HvresMeaningDictionary()
    {
    }
}
