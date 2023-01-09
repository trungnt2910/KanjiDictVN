using HtmlAgilityPack;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Generator;

internal static class Utilities
{
    public static T? GetOrDeserialize<T>(this object obj)
    {
        switch (obj)
        {
            case JsonNode node:
                return node.Deserialize<T>();
            case JsonElement element:
                return element.Deserialize<T>();
            case JsonDocument document:
                return document.Deserialize<T>();
            default:
                return (T)obj;
        }
    }

    public static string GetPlainText(this HtmlNode htmlNode)
    {
        var html = htmlNode.OuterHtml;

        //first - remove all the existing '\n' from HTML
        //they mean nothing in HTML, but break our logic
        html = html.Replace("\r", "").Replace("\n", " ");

        //now create an Html Agile Doc object
        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);

        //remove comments, head, style and script tags
        foreach (HtmlNode node in doc.DocumentNode.SafeSelectNodes("//comment() | //script | //style | //head"))
        {
            node.ParentNode.RemoveChild(node);
        }

        //now remove all "meaningless" inline elements like "span"
        foreach (HtmlNode node in doc.DocumentNode.SafeSelectNodes("//span | //label")) //add "b", "i" if required
        {
            node.ParentNode.ReplaceChild(HtmlNode.CreateNode(node.InnerHtml), node);
        }

        //block-elements - convert to line-breaks
        foreach (HtmlNode node in doc.DocumentNode.SafeSelectNodes("//p | //div")) //you could add more tags here
        {
            //we add a "\n" ONLY if the node contains some plain text as "direct" child
            //meaning - text is not nested inside children, but only one-level deep

            //use XPath to find direct "text" in element
            var txtNode = node.SelectSingleNode("text()");

            //no "direct" text - NOT ADDDING the \n !!!!
            if (txtNode == null || txtNode.InnerHtml.Trim() == "") continue;

            //"surround" the node with line breaks
            node.ParentNode.InsertBefore(doc.CreateTextNode(Environment.NewLine), node);
            node.ParentNode.InsertAfter(doc.CreateTextNode(Environment.NewLine), node);
        }

        //todo: might need to replace multiple "\n\n" into one here, I'm still testing...

        //now BR tags - simply replace with "\n" and forget
        foreach (HtmlNode node in doc.DocumentNode.SafeSelectNodes("//br"))
            node.ParentNode.ReplaceChild(doc.CreateTextNode(Environment.NewLine), node);

        //finally - return the text which will have our inserted line-breaks in it
        return doc.DocumentNode.InnerText.Trim();

        //todo - you should probably add "&code;" processing, to decode all the &nbsp; and such
    }

    private static HtmlNodeCollection SafeSelectNodes(this HtmlNode node, string selector)
    {
        return (node.SelectNodes(selector) ?? new HtmlNodeCollection(node));
    }
}
