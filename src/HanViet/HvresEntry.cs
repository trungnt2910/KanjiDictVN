using HtmlAgilityPack;
using System.Diagnostics;

namespace Generator.HanViet;

[DebuggerDisplay("{Index} - {Name}")]
public class HvresEntry : HvresBase
{
    public string Name { get; set; }
    public int Index { get; set; }

    public HvresEntry(HtmlNode node) : base(node)
    {
        Name = node.Attributes["name"].Value;
        Index = int.Parse(node.Attributes["data-hvres-idx"].Value);
    }

    public HvresEntry()
    {
        Name = string.Empty;
    }
}
