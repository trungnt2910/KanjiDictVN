using HtmlAgilityPack;

namespace Generator.HanViet;

public class HvresDefinition
{
    public HvresSpell? Spell { get; set; }
    public HvresInfo? Info { get; set; }

    public HvresDefinition(HtmlNode node)
    {
        if (!node.HasClass("hvres-definition"))
        {
            throw new ArgumentException("Must be a hvres-definition node.");
        }

        var children = node.Children().ToList();

        if (children.Count > 2)
        {
            throw new ArgumentException("hvres-definition nodes are not known to have more than two children.");
        }

        foreach (var child in children)
        {
            if (child.HasClass("hvres-spell"))
            {
                Spell = new HvresSpell(child);
            }
            else if (child.HasClass("hvres-info"))
            {
                Info = new HvresInfo(child);
            }
            else
            {
                throw new ArgumentException($"Node contains an unknown child: {string.Join(" ", child.GetClasses())}");
            }
        }
    }

    public HvresDefinition()
    {

    }
}
