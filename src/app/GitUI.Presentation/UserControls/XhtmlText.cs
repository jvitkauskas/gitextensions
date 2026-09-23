using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace GitUI.Presentation.UserControls;

/// <summary>The text of the XHTML subset of the commit info (as the text of the WinForms <c>RichTextBox</c> showing it).</summary>
public static partial class XhtmlText
{
    [GeneratedRegex("<[^>]*>")]
    private static partial Regex TagRegex { get; }

    /// <summary>The text without the markup: <c>br</c> and paragraphs are line breaks, entities are decoded.</summary>
    public static string ToPlainText(string xhtml)
    {
        StringBuilder text = new();
        try
        {
            XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true, ConformanceLevel = ConformanceLevel.Fragment };
            using XmlReader reader = XmlReader.Create(new StringReader(xhtml), settings);
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        string tag = reader.Name.ToLowerInvariant();
                        if (tag == "br" || (tag == "p" && !reader.IsEmptyElement && text.Length > 0))
                        {
                            text.Append('\n');
                        }

                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.CDATA:
                        text.Append(reader.Value.Replace("\r\n", "\n"));
                        break;
                }
            }
        }
        catch (XmlException)
        {
            // Not well formed: the text without the markup (as XhtmlTextBlock).
            return WebUtility.HtmlDecode(TagRegex.Replace(xhtml, ""));
        }

        return text.ToString();
    }
}
