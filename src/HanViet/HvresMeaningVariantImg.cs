using HtmlAgilityPack;

namespace Generator.HanViet;

public class HvresMeaningVariantImg : HvresMeaning
{
    public List<KeyValuePair<string, string>> Variants { get; set; } = new();

    public HvresMeaningVariantImg(HtmlNode node) : base(node)
    {
        var children = node.Children().ToList();

        foreach (var child in children)
        {
            var key = child.Attributes["data-tippy-content"]?.Value;
            // Some versions use "title" or "data-tooltip" instead.
            key ??= child.Attributes["title"]?.Value;
            key ??= child.Attributes["data-tooltip"].Value;

            var value = child.Children().Single().Attributes["data-original"].Value;

            Variants.Add(new KeyValuePair<string, string>(key, value));
        }
    }

    public HvresMeaningVariantImg()
    {
    }
}
