using HtmlAgilityPack;
using System.Diagnostics;
using System.Net;

namespace Generator.HanViet;

[DebuggerDisplay("{Value}")]
public class HvresVariant
{
    public string Value { get; set; } = string.Empty;

    public HvresVariant(HtmlNode node)
    {
        if (!node.HasClass("hvres-variant"))
        {
            throw new ArgumentException("Must be a hvres-variant node.");
        }

        // Node may be HTML encoded.
        Value = WebUtility.HtmlDecode(node.GetPlainText());
    }

    public HvresVariant()
    {

    }
}
