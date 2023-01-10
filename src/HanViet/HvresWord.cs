using HtmlAgilityPack;
using System.Diagnostics;
using System.Net;

namespace Generator.HanViet;

[DebuggerDisplay("{Value}")]
public class HvresWord
{
    public string Value { get; set; } = string.Empty;

    public HvresWord(HtmlNode node)
    {
        if (!node.HasClass("hvres-word"))
        {
            throw new ArgumentException("Must be a hvres-word node.");
        }

        Value = WebUtility.HtmlDecode(node.GetPlainText());
    }

    public HvresWord()
    {

    }
}
