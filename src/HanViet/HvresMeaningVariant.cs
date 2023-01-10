using HtmlAgilityPack;

namespace Generator.HanViet;

public class HvresMeaningVariant : HvresMeaning
{
    public List<HvresVariant> Variants { get; set; } = new();

    public HvresMeaningVariant(HtmlNode node) : base(node)
    {
        var aChildren = node.Children().Where(c => c.Name == "a").ToList();

        foreach (var child in aChildren)
        {
            var childChildren = child.Children().ToList();

            if (childChildren.Count != 1)
            {
                throw new ArgumentException("Invalid HvresMeaningVariant section.");
            }

            var childChild = childChildren[0];
            Variants.Add(new HvresVariant(childChild));
        }
    }

    public HvresMeaningVariant()
    {
    }
}
