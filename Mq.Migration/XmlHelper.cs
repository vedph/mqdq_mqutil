using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Mq.Migration
{
    internal static class XmlHelper
    {
        public static readonly XNamespace TEI = "http://www.tei-c.org/ns/1.0";
        public static readonly XNamespace XML = "http://www.w3.org/XML/1998/namespace";

        public static XElement GetTeiBody(XDocument doc)
        {
            if (doc == null)
                throw new System.ArgumentNullException(nameof(doc));

            return doc.Root
                .Element(TEI + "text")
                .Element(TEI + "body");
        }

        private static string GetAttributeName(XAttribute a) =>
            (a.Name.Namespace == XML ? "xml:" : "") + a.Name.LocalName;

        private static string ConcatDivAttributes(XElement div) =>
            string.Join("\u2016",
            div.Attributes().Select(a => $"{GetAttributeName(a)}={a.Value}"));

        public static string GetBreakPointCitation(XElement firstChild,
            string docId)
        {
            StringBuilder sb = new StringBuilder();

            // filename
            sb.Append(docId);

            // div1 attrs
            XElement div1 = firstChild.Ancestors(TEI + "div1").First();
            sb.Append(' ').Append(ConcatDivAttributes(div1));

            // div2 attrs if any
            XElement div2 = div1.Descendants(TEI + "div2").LastOrDefault();
            if (div2 != null)
            {
                sb.Append(' ').Append(ConcatDivAttributes(div2));
            }

            // line number # line ID
            sb.Append(' ')
              .Append(firstChild.Attribute("n").Value)
              .Append('#')
              .Append(firstChild.Attribute(XML + "id").Value);

            return sb.ToString();
        }
    }
}
