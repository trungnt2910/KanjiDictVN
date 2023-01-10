using HtmlAgilityPack;
using System.Diagnostics;
using System.Net;

namespace Generator.HanViet;

[DebuggerDisplay("{Value}")]
public class HvresSpell
{
    public string Value { get; set; } = string.Empty;

    public HvresSpell(HtmlNode node)
    {
        if (!node.HasClass("hvres-spell"))
        {
            throw new ArgumentException("Must be a hvres-spell node.");
        }

        Value = WebUtility.HtmlDecode(node.GetPlainText());
    }

    public HvresSpell()
    {

    }
}
