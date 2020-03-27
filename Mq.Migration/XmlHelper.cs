using System;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// XML helper.
    /// </summary>
    public static class XmlHelper
    {
        public static string CIT_SEPARATOR = "\u2016";
        public static readonly XNamespace TEI = "http://www.tei-c.org/ns/1.0";
        public static readonly XNamespace XML = "http://www.w3.org/XML/1998/namespace";

        /// <summary>
        /// Gets the TEI body.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <returns>Body element.</returns>
        /// <exception cref="ArgumentNullException">doc</exception>
        public static XElement GetTeiBody(XDocument doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            return doc.Root
                .Element(TEI + "text")
                .Element(TEI + "body");
        }

        private static string GetAttributeName(XAttribute a) =>
            (a.Name.Namespace == XML ? "xml:" : "") + a.Name.LocalName;

        private static string ConcatDivAttributes(XElement div) =>
            string.Join(CIT_SEPARATOR,
            div.Attributes().Select(a => $"{GetAttributeName(a)}={a.Value}"));

        /// <summary>
        /// Gets the break point citation.
        /// </summary>
        /// <param name="partitionNr">The partition ordinal number or 0 to
        /// avoid inserting it.</param>
        /// <param name="firstChild">The first child.</param>
        /// <param name="docId">The document identifier.</param>
        /// <returns>The citation.</returns>
        public static string GetBreakPointCitation(
            int partitionNr,
            XElement firstChild,
            string docId)
        {
            StringBuilder sb = new StringBuilder();

            // filename
            sb.Append(docId);

            // partition number
            if (partitionNr > 0)
                sb.Append(' ').Append(partitionNr.ToString("00000"));

            // line number # line ID
            sb.Append(' ')
              .Append(firstChild.Attribute("n").Value)
              .Append('#')
              .Append(firstChild.Attribute(XML + "id").Value);

            //// div1 attrs
            //XElement div1 = firstChild.Ancestors(TEI + "div1").First();
            //sb.Append(' ').Append(ConcatDivAttributes(div1));

            //// div2 attrs if any
            //XElement div2 = div1.Descendants(TEI + "div2").LastOrDefault();
            //if (div2 != null)
            //{
            //    sb.Append(' ').Append(ConcatDivAttributes(div2));
            //}

            return sb.ToString();
        }

        /// <summary>
        /// Gets the line information into a formatted string.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>String with Y,X or empty if no line info.</returns>
        public static string GetLineInfo(XElement element) =>
            element is IXmlLineInfo info
                ? $"{info.LineNumber},{info.LinePosition}" : "";
    }
}
