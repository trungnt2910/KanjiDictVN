using HtmlAgilityPack;
using System.Diagnostics;

namespace Generator.HanViet;

[DebuggerDisplay("{Value}")]
public class HvresInfo
{
    public string Value { get; set; } = string.Empty;

    public HvresInfo(HtmlNode node)
    {
        if (!node.HasClass("hvres-info"))
        {
            throw new ArgumentException("Must be a hvres-info node.");
        }

        Value = node.GetPlainText();
    }

    public HvresInfo()
    {

    }
}
