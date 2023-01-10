using HtmlAgilityPack;
using System.Diagnostics;
using System.Net;

namespace Generator.HanViet;

[DebuggerDisplay("{Name} ({Badge})")]
public class HvresSource
{
    public string Name { get; set; }
    public string? Badge { get; set; }

    public HvresSource(HtmlNode node)
    {
        if (!node.HasClass("hvres-source"))
        {
            throw new ArgumentException("Must be a hvres-source node.");
        }

        var clonedNode = node.CloneNode(deep: true);
        var badgeNodes = clonedNode.Children().Where(c => c.HasClass("badge")).ToList();

        if (badgeNodes.Count > 1)
        {
            throw new ArgumentException("hvres-source is not known to have more than one badge.");
        }

        var badgeNode = badgeNodes.FirstOrDefault();

        if (badgeNode != null)
        {
            Badge = badgeNode.GetPlainText();
            clonedNode.RemoveChild(badgeNode);
        }

        Name = WebUtility.HtmlDecode(clonedNode.GetPlainText());
    }

    public HvresSource()
    {
        Name = string.Empty;
    }
}
