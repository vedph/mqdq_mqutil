using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// The content of a variant being parsed from an apparatus XML document.
    /// </summary>
    public sealed class XmlApparatusVarContent : IHasLogger
    {
        private readonly Regex _emphRegex;
        private readonly Regex _lbRegex;
        private readonly Regex _wsRegex;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the ident's elements values and IDs.
        /// </summary>
        public IList<string> Idents { get; set; }

        /// <summary>
        /// Gets the notes.
        /// </summary>
        public IList<XmlApparatusNote> Notes { get; }

        /// <summary>
        /// Gets or sets the text value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlApparatusVarContent"/>
        /// class.
        /// </summary>
        public XmlApparatusVarContent()
        {
            _emphRegex = new Regex(@"<(?<c>/?)emph(?:\s+(?<a>[^>]*))?>");
            _lbRegex = new Regex(@"<lb\s*/>");
            _wsRegex = new Regex(@"\s+");

            Idents = new List<string>();
            Notes = new List<XmlApparatusNote>();
        }

        /// <summary>
        /// Removes the formatting provided by <c>emph</c> and <c>lb</c>
        /// elements from the specified element, replacing them with escapes,
        /// and flatten and normalize whitespaces.
        /// </summary>
        /// <param name="element">The content.</param>
        /// <returns>Purged element.</returns>
        /// <exception cref="ArgumentNullException">content</exception>
        public XElement RemoveFormatting(XElement element)
        {
            if (element == null) throw new ArgumentNullException(nameof(element));

            if (!element.HasElements) return element;

            string xml = element.ToString(SaveOptions.DisableFormatting);

            xml = _lbRegex.Replace(xml, "{lb}");
            xml = _emphRegex.Replace(xml, (Match m) =>
            {
                if (m.Groups["c"].Length > 0) return "{/f}";
                switch (m.Groups["a"].Value.Trim())
                {
                    case "style=\"font-style:italic\"":
                        return "{f=i}";
                    case "style=\"font-weight:bold\"":
                        return "{f=b}";
                    case "style=\"vertical-align:super;font-size:smaller\"":
                        return "{f=u}";
                    case "style=\"vertical-align:sub;font-size:smaller\"":
                        return "{f=d}";
                    default:
                        Logger?.LogError("Unexpected emph style: {Style}",
                            m.Groups["a"].Value);
                        return m.Value;
                }
            });
            xml = _wsRegex.Replace(xml, " ");

            return XElement.Parse(xml, LoadOptions.PreserveWhitespace);
        }

        /// <summary>
        /// Adds data from the specified <c>ident</c> element.
        /// </summary>
        /// <param name="identElem">The ident element.</param>
        /// <exception cref="ArgumentNullException">identElem</exception>
        public void AddIdent(XElement identElem)
        {
            if (identElem == null) throw new ArgumentNullException(nameof(identElem));
            Idents.Add($"{identElem.Value.Trim()}#{identElem.Attribute("n").Value}");
        }

        public void AddAnnotation(XElement annElem)
        {
            if (annElem == null) throw new ArgumentNullException(nameof(annElem));

            XElement e = RemoveFormatting(annElem);
            if (e.HasElements)
            {
                Logger?.LogError($"Unexpected variant children element: \"{e}\"");
            }
            int sectionId = 0;
            string type = annElem.Attribute("type")?.Value;

            switch (e.Name.LocalName)
            {
                case "add":
                    if (type == "abstract") sectionId = 1;
                    else if (type == "intertext") sectionId = 4;
                    break;
                case "note":
                    if (type == "operation") sectionId = 2;
                    else if (type == "details") sectionId = 3;
                    break;
            }
            if (sectionId > 0)
            {
                Notes.Add(new XmlApparatusNote
                {
                    SectionId = sectionId,
                    Target = e.Attribute("target")?.Value?.Substring(1),
                    Value = e.Value.Trim()
                });
            }
        }
    }
}
