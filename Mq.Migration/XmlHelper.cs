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

        /// <summary>
        /// Gets the break point citation.
        /// </summary>
        /// <param name="partitionNr">The partition ordinal number or 0 to
        /// avoid inserting it.</param>
        /// <param name="div">The parent div element.</param>
        /// <param name="docId">The document identifier.</param>
        /// <returns>The citation.</returns>
        public static string GetBreakPointCitation(
            int partitionNr,
            XElement div,
            string docId)
        {
            StringBuilder sb = new StringBuilder();

            // filename (e.g. "VERG-eclo")
            sb.Append(docId);

            // partition number (e.g. " 00001")
            if (partitionNr > 0)
                sb.Append(' ').Append(partitionNr.ToString("00000"));

            // div ID (e.g. " #d001")
            sb.Append(" #").Append(div.Attribute(XML + "id").Value);

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
