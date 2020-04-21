using Cadmus.Core;
using Cadmus.Parts.General;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// <summary>
        /// The name of the data key representing text (<c>text</c>).
        /// </summary>
        public const string KEY_TEXT = "text";

        /// <summary>
        /// The name of the data key added to notify that the unique
        /// text has been split into several words (<c>_split</c>).
        /// </summary>
        public const string KEY_SPLIT = "_split";

        /// <summary>
        /// The name of the data key added to preserve the original element
        /// name (this can be either <c>l</c> or <c>p</c>; <c>_name</c>).
        /// </summary>
        public const string KEY_NAME = "_name";

        /// <summary>
        /// The name of the data key used to preserve a legacy escape as a
        /// patch (<c>patch</c>).
        /// </summary>
        public const string KEY_PATCH = "patch";

        private readonly Regex _escRegex;
        private readonly IItemSortKeyBuilder _sortKeyBuilder;
        private string _docId;
        private string _facetId;
        private string _userId;
        private int _partitionNr;

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
            _facetId = "default";
            _userId = "zeus";
            _escRegex = new Regex(@"\(==([^)]+)\)");
            _sortKeyBuilder = new StandardItemSortKeyBuilder();
        }

        private static bool IsLOrP(XElement e) =>
            e.Name == XmlHelper.TEI + "l" || e.Name == XmlHelper.TEI + "p";

        private string GetKeyFromAttrName(XName name)
        {
            // the only xml: attr should be xml:id, which gets translated
            // into "id" for a better user experience
            if (name.Namespace == XmlHelper.XML)
            {
                if (name.LocalName == "id") return "id";
                // this should not happen, anyway don't lose information
                return "xml_" + name;
            }

            return name.LocalName;
        }

        private Tuple<string, string> GetTextAndPatch(string text)
        {
            Match m = _escRegex.Match(text);
            if (m.Success)
            {
                StringBuilder sb = new StringBuilder();
                // text before esc
                if (m.Index > 0) sb.Append(text, 0, m.Index);
                // text after esc
                if (m.Index + m.Length < text.Length)
                {
                    sb.Append(text, m.Index + m.Length,
                        text.Length - (m.Index + m.Length));
                }

                return Tuple.Create(
                    sb.ToString(),
                    m.Groups[1].Value);
            }
            return null;
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
                var textAndPatch = GetTextAndPatch(w.Value);
                if (textAndPatch != null)
                {
                    tile.Data[KEY_TEXT] = textAndPatch.Item1;
                    tile.Data[KEY_PATCH] = textAndPatch.Item2;
                }
                else tile.Data[KEY_TEXT] = w.Value;
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
                var textAndPatch = GetTextAndPatch(wi.Item1);
                if (textAndPatch != null)
                {
                    tile.Data[KEY_TEXT] = textAndPatch.Item1;
                    tile.Data[KEY_PATCH] = textAndPatch.Item2;
                }
                else tile.Data[KEY_TEXT] = wi.Item1;

                tile.Data["id"] = wi.Item2;
                row.Tiles.Add(tile);
            }
        }

        private Tuple<string, string> GetPartitionNBoundaries(IList<XElement> rows)
        {
            XElement first = rows.FirstOrDefault(e => IsLOrP(e)
                             && e.Attribute("n") != null);
            XElement last = rows.LastOrDefault(e => IsLOrP(e)
                            && e.Attribute("n") != null);

            // this should not happen
            if (first == null || last == null) return null;

            return Tuple.Create(first.Attribute("n").Value,
                                last.Attribute("n").Value);
        }

        private string GetPartTextDescription(TiledTextPart part)
        {
            StringBuilder sb = new StringBuilder();

            // collect head text
            int ya = 0, xa = 0;
            foreach (TextTileRow row in part.Rows)
            {
                ya++;
                xa = 0;
                foreach (TextTile tile in row.Tiles)
                {
                    xa++;
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(tile.Data[KEY_TEXT]);
                    if (sb.Length >= 40) goto tail;
                }
            }
            tail:
            // collect tail text
            int i = sb.Length;
            for (int yb = part.Rows.Count; yb >= ya; yb--)
            {
                for (int xb = part.Rows[yb - 1].Tiles.Count;
                     yb == ya? xb > xa : xb > 0;
                     xb--)
                {
                    sb.Insert(i, part.Rows[yb - 1].Tiles[xb - 1].Data[KEY_TEXT]);
                    sb.Insert(i, ' ');
                    if (sb.Length - i >= 40)
                    {
                        sb.Insert(i, "... ");
                        goto end;
                    }
                }
            }
            end:
            return sb.ToString();
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
            IList<XElement> rowElements)
        {
            _partitionNr++;

            // build citation
            string cit = XmlHelper.GetBreakPointCitation(_partitionNr, div, _docId);

            // item
            var nBounds = GetPartitionNBoundaries(rowElements);
            Item item = new Item
            {
                Title = cit,
                FacetId = _facetId,
                GroupId = _docId,
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
            item.Parts.Add(part);

            string divId = div.Attribute(XmlHelper.XML + "id")?.Value;
            int y = 1;
            int wordNr = 1;
            foreach (XElement rowElement in rowElements)
            {
                // row
                TextTileRow row = new TextTileRow
                {
                    Y = y++
                };
                row.Data[KEY_NAME] = rowElement.Name.LocalName;

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
                    // the word ID should be equal to "d" + divID + "w" + N;
                    // if we have no div ID (which should not happen), use
                    // the div ordinal number to build one
                    string idPrefix;
                    if (divId == null)
                    {
                        int divNr = div.ElementsBeforeSelf(div.Name).Count() + 1;
                        idPrefix = $"d{divNr:000}";
                    }
                    else idPrefix = divId;

                    row.Data[KEY_SPLIT] = "1";
                    var wordAndIds = from w in rowElement.Value.Split(' ')
                                     select Tuple.Create(
                                         w,
                                         $"{idPrefix}w{wordNr++}");
                    AddTiles(wordAndIds, row);
                }

                part.Rows.Add(row);
            }

            item.Description = (nBounds != null ?
                    $"{nBounds.Item1}-{nBounds.Item2} " : "")
                    + GetPartTextDescription(part);

            item.SortKey = _sortKeyBuilder.BuildKey(item, null);
            int i = item.SortKey.IndexOf(' ', item.SortKey.IndexOf(' ') + 1);
            item.SortKey = item.SortKey.Substring(0, i);

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
                    div.Elements().Where(IsLOrP).ToList());
            }
        }

        private IEnumerable<IItem> ImportPartitionedText(XDocument doc)
        {
            XElement body = XmlHelper.GetTeiBody(doc);

            foreach (XElement div in body.Elements(XmlHelper.TEI + "div1"))
            {
                // partition extends up to first pb or up to div's end
                IEnumerable<XElement> rows;
                XElement first = div.Elements().FirstOrDefault(IsLOrP);
                XElement pb = div.Elements(XmlHelper.TEI + "pb").FirstOrDefault();

                while (first != null)
                {
                    if (pb != null)
                    {
                        rows = div.Elements()
                            .SkipWhile(e => e != first)
                            .TakeWhile(e => e != pb)
                            .Where(IsLOrP);
                    }
                    else
                    {
                        rows = div.Elements()
                            .SkipWhile(e => e != first)
                            .Where(IsLOrP);
                    }

                    yield return ImportPartition(div, rows.ToList());

                    // next sibling l/p after pb is now the first element
                    first = pb?.ElementsAfterSelf().FirstOrDefault(IsLOrP);
                    // next pb after new first element
                    pb = first?.ElementsAfterSelf()
                        .FirstOrDefault(e => e.Name == XmlHelper.TEI + "pb");
                }
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

            _partitionNr = 0;

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
