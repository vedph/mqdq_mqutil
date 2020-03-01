using Cadmus.Core.Config;
using Fusi.Tools.Text;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Mq.Migration
{
    public sealed class XmlThesaurusParser : IHasLogger
    {
        private readonly TextCutterOptions _options;
        private readonly Regex _tailRegex;
        private readonly Regex _wsRegex;

        public XmlThesaurusParser()
        {
            _options = new TextCutterOptions
            {
                LimitAsPercents = false,
                LineFlattening = true,
                MaxLength = 30,
                MinusLimit = 5,
                PlusLimit = 5
            };
            _tailRegex = new Regex(@"\s*(?<p>\([^)]+\))|(?<p>\[[^]]+\])\s*$");
            _wsRegex = new Regex(@"\s+");
        }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        private string ReduceLabel(string label)
        {
            if (label.Length <= _options.MaxLength) return label;

            // extract tail
            string tail = "";
            Match m = _tailRegex.Match(label);
            if (m.Success)
            {
                tail = m.Groups[1].Value;
                int tailLen = tail.Length;
                tail = TextCutter.Cut(tail, _options);
                if (tail.Length < tailLen)
                    tail += tail[1] == '[' ? ']' : ')';

                label = label.Substring(0, m.Index);
            }

            string head = TextCutter.Cut(label, _options);

            return head + (tail.Length > 0? (" " + tail) : "");
        }

        private Thesaurus ParseSource(XElement sourceElem, string id,
            bool authors)
        {
            Thesaurus thesaurus = new Thesaurus(
                $"apparatus-{(authors? "authors":"witnesses")}.{id}@en");

            foreach (XElement child in sourceElem
                .Elements(XmlHelper.TEI + (authors? "bibl" : "witness")))
            {
                string value = _wsRegex.Replace(child.Value, " ").Trim();

                // prepend @n if @ref
                if (child.Attribute("ref") != null)
                    value = child.Attribute("n").Value + value;

                var entry = new ThesaurusEntry(
                    child.Attribute(XmlHelper.XML + "id").Value,
                    ReduceLabel(value));

                thesaurus.AddEntry(entry);
            }

            return thesaurus;
        }

        public Thesaurus[] Parse(XDocument doc, string id)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (id == null) throw new ArgumentNullException(nameof(id));

            id = id.ToLowerInvariant();

            XElement sourceDescElem = doc.Root
                .Element(XmlHelper.TEI + "teiHeader")
                .Element(XmlHelper.TEI + "fileDesc")
                .Elements(XmlHelper.TEI + "sourceDesc")
                .FirstOrDefault(e => e.Elements()
                    .Any(c => c.Name == XmlHelper.TEI + "listWit"));

            Thesaurus witnesses = ParseSource(sourceDescElem
                .Element(XmlHelper.TEI + "listWit")
                .Element(XmlHelper.TEI + "listWit"),
                id, false);

            Thesaurus authors = ParseSource(sourceDescElem
                .Element(XmlHelper.TEI + "listBibl")
                .Element(XmlHelper.TEI + "listBibl"),
                id, true);

            return new[] { witnesses, authors };
        }
    }
}
