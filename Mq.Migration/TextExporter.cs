﻿using Cadmus.Core;
using Cadmus.Core.Storage;
using Cadmus.Parts.General;
using Fusi.Tools.Data;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// Text exporter. This extracts the text from database items, and
    /// injects it back into the corresponding TEI text documents.
    /// </summary>
    public sealed class TextExporter : IHasLogger
    {
        private readonly ICadmusRepository _repository;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include some comments
        /// in the output XML. These can be used for diagnostic purposes.
        /// </summary>
        public bool IncludeComments { get; set; }

        public TextExporter(ICadmusRepository repository)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        private async Task<bool> HasWordGranularityAsync(XDocument doc,
            string groupId)
        {
            // if the document had any w, we must output w
            XElement body = XmlHelper.GetTeiBody(doc);
            if (body.Descendants(XmlHelper.TEI + "w").Any())
                return true;

            // else we must query the database to find out if any of
            // the items corresponding to the document has layers
            int layerCount = await _repository.GetGroupLayersCountAsync(groupId);
            return layerCount > 0;
        }

        /// <summary>
        /// Clears the div contents by removing all the children elements
        /// except for the initial <c>head</c> and any other <c>div</c>'s.
        /// </summary>
        /// <param name="div">The div element to be cleared.</param>
        private void ClearDivContents(XElement div)
        {
            XElement head = div.Elements(XmlHelper.TEI + "head").FirstOrDefault();
            if (head != null)
            {
                if (head.ElementsBeforeSelf().Any())
                {
                    Logger?.LogError(
                        $"Unexpected elements before head in {div.Name.LocalName}"
                        + " at " + XmlHelper.GetLineInfo(div));
                }
                foreach (XElement sibling in head.ElementsAfterSelf()
                    .Where(e => !e.Name.LocalName.StartsWith("div")).ToList())
                {
                    sibling.Remove();
                }
            }
            else
            {
                foreach (XElement child in div.Elements()
                    .Where(e => !e.Name.LocalName.StartsWith("div")).ToList())
                {
                    child.Remove();
                }
            }
        }

        private void ClearDocDivContents(XDocument doc)
        {
            XElement body = XmlHelper.GetTeiBody(doc);

            foreach (XElement div in body.Descendants(XmlHelper.TEI + "div2"))
            {
                ClearDivContents(div);
            }
        }

        private void AppendItemContent(IItem item, XElement div, bool hasWords)
        {
            if (IncludeComments) div.Add(new XComment("item " + item.Id));

            TiledTextPart part = item.Parts.OfType<TiledTextPart>().FirstOrDefault();
            if (part == null)
            {
                Logger?.LogError($"Item {item.Id} has no text part");
                return;
            }

            foreach (TextTileRow row in part.Rows)
            {
                string name = row.Data.ContainsKey("_name") ?
                    row.Data["_name"] : "l";
                XElement lp = new XElement(XmlHelper.TEI + name);

                foreach (var pair in row.Data)
                    lp.SetAttributeValue(pair.Key, pair.Value);

                if (hasWords)
                {
                    foreach (TextTile tile in row.Tiles)
                    {
                        XElement w = new XElement(XmlHelper.TEI + "w",
                            tile.Data["text"]);
                        foreach (var pair in tile.Data.Where(p =>
                            p.Key != "text"
                            && p.Key != "_split"
                            && p.Key != "_name"))
                        {
                            w.SetAttributeValue(pair.Key, pair.Value);
                        }
                        lp.Add(w);
                    }
                }
                else
                {
                    lp.Value = string.Join(" ", from t in row.Tiles
                                               select t.Data["text"]);
                }
            }
        }

        public async Task ExportAsync(string outputDir)
        {
            if (outputDir is null)
                throw new ArgumentNullException(nameof(outputDir));

            PagingOptions groupFilter = new PagingOptions
            {
                PageNumber = 1,
                PageSize = 100
            };
            DataPage<string> groupPage =
                await _repository.GetDistinctGroupIdsAsync(groupFilter);
            Logger?.LogInformation($"Groups found: {groupPage.Total}");

            ItemFilter itemFilter = new ItemFilter
            {
                PageNumber = 1,
                PageSize = 50
            };

            while (groupFilter.PageNumber <= groupPage.PageCount)
            {
                foreach (string groupId in groupPage.Items)
                {
                    Logger?.LogInformation(groupId);

                    // open the target document
                    string filePath = Path.Combine(outputDir, groupId + ".xml");
                    XDocument doc = XDocument.Load(filePath,
                        LoadOptions.PreserveWhitespace |
                        LoadOptions.SetLineInfo);
                    XElement body = XmlHelper.GetTeiBody(doc);

                    // determine if we should output w elements
                    bool hasWordElems = await HasWordGranularityAsync(doc, groupId);

                    // clear the div children except for initial head
                    ClearDocDivContents(doc);

                    // for each item in the group, sorted by key:
                    itemFilter.GroupId = groupId;
                    DataPage<ItemInfo> itemPage = _repository.GetItems(itemFilter);

                    while (itemFilter.PageNumber <= itemPage.PageCount)
                    {
                        foreach (ItemInfo info in itemPage.Items)
                        {
                            // locate the target div element via its @xml:id
                            // (log error and continue if not found)
                            XElement div = body.Descendants()
                                .FirstOrDefault(e => (e.Name.LocalName == "div1"
                                    || e.Name.LocalName == "div2")
                                    && e.Attribute(XmlHelper.XML + "id")?.Value == info.Id);
                            if (div == null)
                            {
                                Logger?.LogError(
                                    $"Target div for item ID {info.Id} not found");
                                continue;
                            }

                            // get the item
                            IItem item = _repository.GetItem(info.Id);

                            // build and append content elements
                            AppendItemContent(item, div, hasWordElems);
                        }
                        if (++itemFilter.PageNumber <= itemPage.PageCount)
                            itemPage = _repository.GetItems(itemFilter);
                    }

                    doc.Save(filePath, SaveOptions.OmitDuplicateNamespaces);
                } // group

                // next groups page
                if (++groupFilter.PageNumber <= groupPage.PageCount)
                    groupPage = await _repository.GetDistinctGroupIdsAsync(groupFilter);
            }
        }
    }
}
