using Cadmus.Core;
using Cadmus.Parts.General;
using Fusi.Tools.Text;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// XML documents parser. This parses XML documents, extracts data
    /// from them, and remodels them into Cadmus entities.
    /// </summary>
    public sealed class XmlTextParser : IHasLogger
    {
        private readonly TextCutterOptions _headOptions;
        private readonly TextCutterOptions _tailOptions;
        private readonly Regex _digitsRegex;
        private string _docId;
        private string _facetId;
        private string _userId;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the facet to be assigned to items.
        /// The default value is <c>default</c>.
        /// </summary>
        public string FacetId
        {
            get { return _facetId; }
            set
            {
                _facetId = value
                    ?? throw new ArgumentNullException(nameof(value));
            }
        }

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
        /// Initializes a new instance of the <see cref="XmlTextParser"/> class.
        /// </summary>
        public XmlTextParser()
        {
            _headOptions = new TextCutterOptions
            {
                LimitAsPercents = false,
                LineFlattening = true,
                MaxLength = 30,
                MinusLimit = 5,
                PlusLimit = 5
            };
            _tailOptions = new TextCutterOptions
            {
                LimitAsPercents = false,
                LineFlattening = true,
                MaxLength = 30,
                MinusLimit = 5,
                PlusLimit = 5,
                Reversed = true,
                Ellipsis = ""
            };
            _facetId = "default";
            _userId = "zeus";
            _digitsRegex = new Regex(@"\d+");
        }

        private static bool IsLOrP(XElement e) =>
            e.Name == XmlHelper.TEI + "l" || e.Name == XmlHelper.TEI + "p";

        private string GetDescriptionText(string text)
        {
            text = _digitsRegex.Replace(text, "");
            text = text.Trim();

            if (text.Length <= 80) return text;
            return TextCutter.Cut(text, _headOptions)
                   + TextCutter.Cut(text, _tailOptions);
        }

        private string GetKeyFromAttrName(XName name)
        {
            // the only xml: attr should be xml:id, which gets translated
            // into "id" for a better user experience
            if (name.Namespace == XmlHelper.XML)
            {
                if (name.LocalName == "id") return "id";
                // this should not happen, anyway don't loose information
                return "xml_" + name;
            }

            Debug.Assert(name.Namespace == XmlHelper.TEI);
            return name.LocalName;
        }

        private void AddTiles(IEnumerable<XElement> wElements, TextTileRow row)
        {
            int x = 1;
            foreach (XElement w in wElements)
            {
                TextTile tile = new TextTile
                {
                    X = x++
                };
                // w's attributes
                foreach (XAttribute attr in w.Attributes())
                {
                    string key = GetKeyFromAttrName(attr.Name);
                    tile.Data[key] = attr.Value;
                }
                tile.Data["text"] = w.Value;
                row.Tiles.Add(tile);
            }
        }

        private void AddTiles(IEnumerable<Tuple<string, string>> wordAndIds,
            TextTileRow row)
        {
            int x = 1;
            foreach (var wi in wordAndIds)
            {
                TextTile tile = new TextTile
                {
                    X = x++
                };
                tile.Data["text"] = wi.Item1;
                tile.Data["id"] = wi.Item2;
                row.Tiles.Add(tile);
            }
        }

        /// <summary>
        /// Imports the partition into a Cadmus item.
        /// </summary>
        /// <param name="div">The document's div element containing the partition,
        /// or a part of it.</param>
        /// <param name="rowElements">The rows elements, i.e. the children elements
        /// of the partition representing tiled text rows (either <c>l</c> or
        /// <c>p</c> elements).</param>
        /// <returns>The item.</returns>
        private IItem ImportPartition(XElement div,
            IEnumerable<XElement> rowElements)
        {
            // build citation
            string cit = XmlHelper.GetBreakPointCitation(
                div.Elements().FirstOrDefault(IsLOrP),
                _docId);

            // item
            Item item = new Item
            {
                Title = cit,
                Description = GetDescriptionText(div.Value.Trim()),
                FacetId = _facetId,
                CreatorId = _userId,
                UserId = UserId
            };

            // text part
            TiledTextPart part = new TiledTextPart
            {
                ItemId = item.Id,
                Citation = cit,
                CreatorId = item.CreatorId,
                UserId = item.UserId,
                RoleId = PartBase.BASE_TEXT_ROLE_ID
            };

            int y = 1;
            int wordNr = 1;
            foreach (XElement rowElement in rowElements)
            {
                // row
                TextTileRow row = new TextTileRow
                {
                    Y = y++
                };
                row.Data["_name"] = rowElement.Name.LocalName;

                // row's attributes
                foreach (XAttribute attr in rowElement.Attributes())
                {
                    string key = GetKeyFromAttrName(attr.Name);
                    row.Data[key] = attr.Value;
                }

                // tiles
                if (rowElement.Elements(XmlHelper.TEI + "w").Any())
                {
                    AddTiles(rowElement.Elements(XmlHelper.TEI + "w"), row);
                }
                else
                {
                    int divNr = div.ElementsBeforeSelf(div.Name).Count() + 1;
                    row.Data["_split"] = "1";
                    var wordAndIds = from w in rowElement.Value.Split(' ')
                                     select Tuple.Create(
                                         w,
                                         $"d{divNr:000}w{wordNr++}");
                    AddTiles(wordAndIds, row);
                }

                part.Rows.Add(row);
            }

            return item;
        }

        private IEnumerable<IItem> ImportUnpartitionedText(XDocument doc)
        {
            XElement body = XmlHelper.GetTeiBody(doc);
            XName divName = XmlHelper.TEI + (
                body.Descendants(XmlHelper.TEI + "div2").Any() ?
                "div2" : "div1");

            foreach (XElement div in body.Elements(divName))
            {
                yield return ImportPartition(
                    div,
                    div.Elements().Where(IsLOrP));
            }
        }

        private IEnumerable<IItem> ImportPartitionedText(XDocument doc)
        {
            XElement body = XmlHelper.GetTeiBody(doc);

            foreach (XElement div in body.Elements(XmlHelper.TEI + "div1"))
            {
                // partition extends up to first pb or up to div's end
                IEnumerable<XElement> rows;
                XElement pb = div.Elements(XmlHelper.TEI + "pb").FirstOrDefault();

                if (pb != null)
                {
                    rows = div.Elements()
                        .TakeWhile(e => e != pb)
                        .Where(IsLOrP);
                }
                else
                {
                    rows = div.Elements().Where(IsLOrP);
                }

                yield return ImportPartition(div, rows);
            }
        }

        /// <summary>
        /// Parse text from the specified document.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="id">The document ID (=its filename, no extension).</param>
        /// <returns>Items with parts.</returns>
        /// <exception cref="ArgumentNullException">doc or id</exception>
        public IEnumerable<IItem> Parse(XDocument doc, string id)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            _docId = id ?? throw new ArgumentNullException(nameof(id));

            if (doc.Root.Descendants(XmlHelper.TEI + "pb").Any())
            {
                foreach (IItem item in ImportPartitionedText(doc))
                    yield return item;
            }
            else
            {
                foreach (IItem item in ImportUnpartitionedText(doc))
                    yield return item;
            }
        }
    }
}
