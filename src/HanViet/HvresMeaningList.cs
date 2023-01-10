using HtmlAgilityPack;
using System.Net;

namespace Generator.HanViet;

public class HvresMeaningList : HvresMeaning
{
    public List<string> Values { get; set; } = new();

    public HvresMeaningList(HtmlNode node) : base(node)
    {
        var nodeText = node.GetPlainText();
        var nodeTextLines = nodeText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (nodeText.Contains('•'))
        {
            // Bulleted list
            Values.AddRange(nodeText.Split('•', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        else if (nodeTextLines.All(l => 
            (char.IsNumber(l[0]) && !char.IsDigit(l[0])) 
            || (l.Contains('.') && l.Substring(0, l.IndexOf('.')).All(c => char.IsNumber(c)))))
        {
            // Numbered list
            foreach (var line in nodeTextLines)
            {
                var newLine = new string(line.SkipWhile(char.IsNumber).ToArray())
                    .TrimStart('.')
                    .Trim();

                Values.Add(WebUtility.HtmlDecode(newLine));
            }
        }
        else
        {
            // Plain text.
            Values.Add(WebUtility.HtmlDecode(nodeText).Trim());
        }
    }

    public HvresMeaningList()
    {
    }
}
