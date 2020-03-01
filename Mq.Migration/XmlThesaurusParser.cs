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
            _wsRegex = new Regex(@"\s+");
        }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        private string ReduceLabel(string label)
        {
            label = _wsRegex.Replace(label, " ").Trim();
            int i = label.LastIndexOf('[');
            string head = TextCutter.Cut(label, _options);
            return i > -1 ? head + (label.Substring(i)) : head;
        }

        private Thesaurus ParseSource(XElement sourceElem, string id,
            bool authors)
        {
            Thesaurus thesaurus = new Thesaurus(
                $"apparatus-{(authors? "authors":"witnesses")}.{id}@en");

            foreach (XElement child in sourceElem
                .Elements(XmlHelper.TEI + (authors? "bibl" : "witness")))
            {
                var entry = new ThesaurusEntry(
                    child.Attribute(XmlHelper.XML + "id").Value,
                    ReduceLabel(child.Value));
                if (entry.Value.Length == 0)
                {
                    Logger?.LogWarning(
                        $"Empty entry for ID {entry.Id} in document {id}");
                }
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
