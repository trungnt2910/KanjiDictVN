using HtmlAgilityPack;

namespace Generator.HanViet;

public class HvresDetails
{
    public HvresMeaning? Overview { get; set; }

    public List<KeyValuePair<HvresSource, HvresMeaning>> Meanings { get; set; } = new();

    public HvresDetails(HtmlNode node)
    {
        if (!node.HasClass("hvres-details"))    
        {
            throw new ArgumentException("Must be a hvres-details node.");
        }

        var children = node.Children()
            // Workaround for hvdic bug.
            .SkipWhile(c => c.Name == "" && c.OuterHtml.StartsWith("<p"))
            .ToList();

        if (children.Count == 0)
        {
            return;
        }

        if (children.Count == 1)
        {
            // Some hvres-details nodes contains a single paragraph element which then contains our info.
            node = children[0];
        }

        var currentIndex = 0;

        if (children[0].HasClass("hvres-meaning"))
        {
            // First node is a meaning, meaning this is an overview.
            Overview = HvresMeaning.FromNode(children[0]);
            currentIndex = 1;
        }

        if ((children.Count - currentIndex) % 2 != 0)
        {
            throw new ArgumentException("hvres-details node must contain an even number of elements (excluding the first hvres-meaning node).");
        }

        while (currentIndex < children.Count)
        {
            var firstChild = children[currentIndex];
            var secondChild = children[currentIndex + 1];

            if (!firstChild.HasClass("hvres-source"))
            {
                throw new ArgumentException($"Node contains an unknown child: {string.Join(" ", firstChild.GetClasses())}, expected a hvres-source.");
            }

            if (!secondChild.HasClass("hvres-meaning"))
            {
                throw new ArgumentException($"Node contains an unknown child: {string.Join(" ", secondChild.GetClasses())}, expected a hvres-meaning");
            }

            Meanings.Add(new KeyValuePair<HvresSource, HvresMeaning>(new HvresSource(firstChild), HvresMeaning.FromNode(secondChild)));

            currentIndex += 2;
        }
    }

    public HvresDetails()
    {

    }
}
