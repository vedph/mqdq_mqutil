using Cadmus.Parts.Layers;
using Cadmus.Philology.Parts.Layers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// XML apparatus document parser. This parses an <c>-app</c> TEI document,
    /// using the JSON dump files representing the base text as reference
    /// for calculating the location.
    /// </summary>
    public sealed class XmlApparatusParser : IHasLogger
    {
        private JsonTextIndex _textIndex;
        private string _userId;

        /// <summary>
        /// Gets or sets the user identifier to be assigned to data being
        /// imported. The default value is <c>zeus</c>.
        /// </summary>
        public string UserId
        {
            get { return _userId; }
            set
            {
                _userId = value
                    ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlApparatusParser"/>
        /// class.
        /// </summary>
        public XmlApparatusParser()
        {
            _userId = "zeus";
        }

        private Tuple<string, string> ParseFromTo(XElement appElem)
        {
            string from = appElem.Attribute("from").Value.Substring(1);
            string to = appElem.Attribute("to").Value.Substring(1);

            JsonTextIndexPayload a = _textIndex.Find(from);
            if (a == null)
            {
                Logger?.LogError("Word ID {WordId} not found", from);
                return null;
            }
            // range
            if (from != to)
            {
                JsonTextIndexPayload b = _textIndex.Find(to);
                if (b == null)
                {
                    Logger?.LogError("Word ID {WordId} not found", from);
                    return null;
                }
                if (b.ItemId != a.ItemId)
                {
                    Logger?.LogError("Fragment spans two items: {FromLoc} {ToLoc}",
                        a, b);
                    return null;
                }
                return Tuple.Create(a.ItemId, $"{a.Y}.{a.X}-{b.Y}.{b.X}");
            }
            // point
            return Tuple.Create(a.ItemId, $"{a.Y}.{a.X}");
        }

        private Tuple<string, string[]> ParseLoc(string loc)
        {
            List<string> locs = new List<string>();
            string itemId = null;

            foreach (string token in loc.Split(new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries))
            {
                JsonTextIndexPayload a = _textIndex.Find(token.Substring(1));
                if (itemId == null) itemId = a.ItemId;
                else if (a.ItemId != itemId)
                {
                    Logger?.LogError("Fragment spans two items: {Loc}", loc);
                    return null;
                }
                locs.Add($"{a.Y}.{a.X}");
            }
            return Tuple.Create(itemId, locs.ToArray());
        }

        private void ParseWit(string wit, ApparatusEntry entry)
        {
            if (string.IsNullOrEmpty(wit)) return;

            foreach (string token in wit.Split(new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries))
            {
                entry.Witnesses.Add(new ApparatusAnnotatedValue
                {
                    Value = token.Substring(1)
                });
            }
        }

        private void ParseSource(string source, ApparatusEntry entry)
        {
            if (string.IsNullOrEmpty(source)) return;

            foreach (string token in source.Split(new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries))
            {
                entry.Authors.Add(new ApparatusAnnotatedValue
                {
                    Value = token.Substring(1)
                });
            }
        }

        private XmlApparatusVarContent ParseVariantContent(XElement variant)
        {
            XmlApparatusVarContent content = new XmlApparatusVarContent
            {
                Logger = Logger
            };

            // scan children elements
            foreach (XElement child in variant.Elements())
            {
                switch (child.Name.LocalName)
                {
                    case "ident":
                        content.AddIdent(child);
                        break;
                    case "add":
                    case "note":
                        content.AddAnnotation(child);
                        break;
                    default:
                        Logger?.LogError("Unexpected element in variant content:" +
                            " {ElementName}", child.Name);
                        break;
                }
            }

            // get direct text
            StringBuilder sb = new StringBuilder();
            foreach (XText txt in variant.Nodes().OfType<XText>())
            {
                sb.Append(txt.Value);
            }
            if (sb.Length > 0) content.Value = sb.ToString();

            return content;
        }

        private void AddContentToEntry(XmlApparatusVarContent content,
            ApparatusEntry entry)
        {
            // TODO
        }

        private TiledTextLayerPart<ApparatusLayerFragment> CreatePart(string docId)
        {
            return new TiledTextLayerPart<ApparatusLayerFragment>
            {
                ThesaurusScope = docId,
                CreatorId = _userId,
                UserId = _userId
            };
        }

        /// <summary>
        /// Parses the specified document.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="id">The document identifier.</param>
        /// <param name="textIndex">The index of the corresponding text.</param>
        /// <returns>Apparatus layer parts.</returns>
        /// <exception cref="ArgumentNullException">doc or id or textIndex
        /// </exception>
        public IEnumerable<TiledTextLayerPart<ApparatusLayerFragment>> Parse(
            XDocument doc, string id, JsonTextIndex textIndex)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (id == null) throw new ArgumentNullException(nameof(id));
            _textIndex = textIndex ??
                throw new ArgumentNullException(nameof(textIndex));

            XElement divElem = doc.Root
                .Element(XmlHelper.TEI + "text")
                .Element(XmlHelper.TEI + "body")
                .Element(XmlHelper.TEI + "div1");

            var part = CreatePart(id);

            // app
            foreach (XElement appElem in divElem.Elements(XmlHelper.TEI + "app"))
            {
                // @type -> tag
                ApparatusLayerFragment fr = new ApparatusLayerFragment
                {
                    Tag = appElem.Attribute("type")?.Value
                };
                string itemId = null;
                string[] locs = null;

                // @from/@to pair provides a single location
                if (appElem.Attribute("from") != null)
                {
                    var t = ParseFromTo(appElem);
                    itemId = t.Item1;
                    fr.Location = t.Item2;
                    if (fr.Location == null) continue;
                }
                // @loc provides multiple locations, each to be assigned
                // to a clone of this fragment; thus, we keep the locations
                // in locs for later use
                else
                {
                    var itemIdAndlocs = ParseLoc(appElem.Attribute("loc").Value);
                    if (itemIdAndlocs == null) continue;
                    itemId = itemIdAndlocs.Item1;
                    locs = itemIdAndlocs.Item2;
                }

                // if the location refers to another item, change part
                if (part.ItemId == null) part.ItemId = itemId;
                else if (part.ItemId != itemId)
                {
                    if (part.Fragments.Count > 0) yield return part;
                    part = CreatePart(id);
                    part.ItemId = itemId;
                }

                // lem, rdg, note
                foreach (XElement child in appElem.Elements())
                {
                    // @type -> tag
                    ApparatusEntry entry = new ApparatusEntry
                    {
                        Tag = child.Attribute("type")?.Value
                    };
                    fr.Entries.Add(entry);
                    XmlApparatusVarContent content = null;

                    switch (child.Name.LocalName)
                    {
                        case "lem":
                            entry.IsAccepted = true;
                            goto case "rdg";
                        case "rdg":
                            // @wit @source
                            ParseWit(child.Attribute("wit")?.Value, entry);
                            ParseSource(child.Attribute("source")?.Value, entry);
                            content = ParseVariantContent(child);
                            AddContentToEntry(content, entry);
                            break;
                        case "note":
                            entry.Type = ApparatusEntryType.Note;
                            content = ParseVariantContent(child);
                            AddContentToEntry(content, entry);
                            break;
                        default:
                            Logger?.LogError("Unexpected element {ElementName} in app",
                                child.Name.LocalName);
                            break;
                    }
                }
                // TODO
            } // app

            if (part.Fragments.Count > 0) yield return part;
            _textIndex = null;
        }
    }
}
