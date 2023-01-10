using HtmlAgilityPack;
using System.Text.Json.Serialization;

namespace Generator.HanViet;

[JsonDerivedType(typeof(HvresMeaningVariant), nameof(HvresMeaningVariant))]
[JsonDerivedType(typeof(HvresMeaningVariantImg), nameof(HvresMeaningVariantImg))]
[JsonDerivedType(typeof(HvresMeaningList), nameof(HvresMeaningList))]
[JsonDerivedType(typeof(HvresMeaningDictionary), nameof(HvresMeaningDictionary))]
public abstract class HvresMeaning
{
    protected HvresMeaning(HtmlNode node)
    {
        if (!node.HasClass("hvres-meaning"))
        {
            throw new ArgumentException("Must be a hvres-meaning node.");
        }
    }

    protected HvresMeaning()
    {
    }

    public static HvresMeaning FromNode(HtmlNode node)
    {
        var isInHanCharacter = false;

        for (var parentNode = node; parentNode != null; parentNode = parentNode.ParentNode)
        {
            if (parentNode.HasClass("han-word"))
            {
                isInHanCharacter = true;
                break;
            }
        }

        var children = node.Children().ToList();

        if (children
            .Where(c => 
                c.Children().Count() == 1 
                && c.Children().First().HasClass("hvres-variant-img"))
            .Count() == children.Count &&
            // Requiring at least one child to prevent text-only nodes from falling
            // into this case.
            children.Count > 0)
        {
            // hvres-variant-img
            return new HvresMeaningVariantImg(node);
        }

        if (node.Descendants()
            .Where(c => c.HasClass("hvres-variant"))
            .Any())
        {
            // hvres-variant
            return new HvresMeaningVariant(node);
        }

        // Text

        var nodeText = node.GetPlainText();
        var nodeTextLines = nodeText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (nodeTextLines.Where(l => l.Where(ch => ch == ':').Count() == 1).Count() == nodeTextLines.Length &&
            // HACK: "Từ điển trích dẫn" also has a dictionary-like form like this:
            //    1. <Definition> ◎Như: <Examples>
            //    2. <Definition> ◎Như: <Examples>
            // but should fit more in the list type.
            // On the other hand, places where the dictionary form should be preferred (overview, "Âm đọc khác") is not known to be numbered.
            nodeTextLines.All(l => !char.IsNumber(l[0])) &&
            // HACK #2: All known occurrences of "MeaningDictionary" lives in the "han-word" entry.
            // This is to prevent some one-line nodes with a colon to be mistaken as a "dictionary".
            isInHanCharacter)
        {
            return new HvresMeaningDictionary(node);
        }

        return new HvresMeaningList(node);
    }
}
