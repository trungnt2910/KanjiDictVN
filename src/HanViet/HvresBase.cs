using HtmlAgilityPack;

namespace Generator.HanViet;

public abstract class HvresBase
{
    public HvresHeader Header { get; set; }
    public HvresDetails Details { get; set; }
    
    protected HvresBase(HtmlNode node)
    {
        if (!node.HasClass("hvres"))
        {
            throw new ArgumentException("Must be a hvres node.");
        }

        var children = node.Children().ToList();

        if (children.Count != 2)
        {
            throw new ArgumentException("A hvres node must have only two children.");
        }

        Header = new HvresHeader(children[0]);
        Details = new HvresDetails(children[1]);
    }

    protected HvresBase()
    {
        Header = new();
        Details = new();
    }
}
