using HtmlAgilityPack;

namespace Generator.HanViet;

public class HvresHeader
{
    public HvresWord? Word { get; set; }
    public HvresDefinition? Definition { get; set; }

    public HvresHeader(HtmlNode node)
    {
        if (!node.HasClass("hvres-header"))
        {
            throw new ArgumentException("Must be a hvres-header node.");
        }

        var children = node.Children().ToList();

        if (children.Count > 2)
        {
            throw new ArgumentException("hvres-header nodes are not known to have more than two children.");
        }

        foreach (var child in children)
        {
            if (child.HasClass("hvres-word"))
            {
                Word = new HvresWord(child);
            }
            else if (child.HasClass("hvres-definition"))
            {
                Definition = new HvresDefinition(child);
            }
            else
            {
                throw new ArgumentException($"Node contains an unknown child: {string.Join(" ", child.GetClasses())}");
            }
        }
    }

    public HvresHeader()
    {

    }
}
