using Cadmus.Core;
using Cadmus.Core.Storage;
using Cadmus.Parts.General;
using Fusi.Tools;
using Fusi.Tools.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// Text exporter. This extracts the text from database items, and
    /// injects it back into the corresponding TEI text documents.
    /// </summary>
    public sealed class TextExporter : ExporterBase
    {
        static private readonly Regex _tailRegex = new Regex(@"[^\p{L}]+$");

        private readonly string _tiledTextPartTypeId;

        public TextExporter(ICadmusRepository repository) : base(repository)
        {
            _tiledTextPartTypeId = new TiledTextPart().TypeId;
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
            int layerCount = await Repository.GetGroupLayersCountAsync(groupId);
            return layerCount > 0;
        }

        private static XAttribute GetAttribute(string name, string value)
        {
            switch (name)
            {
                case XmlTextParser.KEY_NAME:
                case XmlTextParser.KEY_SPLIT:
                case XmlTextParser.KEY_PATCH:
                case XmlTextParser.KEY_TEXT:
                    return null;

                default:
                    // id is a special case, and maps to xml:id
                    if (name == "id")
                        return new XAttribute(XmlHelper.XML + "id", value);
                    // any other XML-namespaced attribute has the xml_ prefix
                    if (name.StartsWith("xml_", StringComparison.Ordinal))
                        return new XAttribute(XmlHelper.XML + name, value);
                    // else it's a TEI attribute
                    return new XAttribute(name, value);
            }
        }

        private static string GetTileText(TextTile tile)
        {
            string text = tile.Data[XmlTextParser.KEY_TEXT];

            if (tile.Data.ContainsKey(XmlTextParser.KEY_PATCH))
            {
                string patch = tile.Data[XmlTextParser.KEY_PATCH];

                // the escape should be inserted before a non-alphabetic tail
                Match m = _tailRegex.Match(text);
                return m.Success
                    ? $"{text.Substring(0, m.Index)}(=={patch}){m.Value}"
                    : $"{text}(=={patch})";
            }

            return text;
        }

        private void AppendItemContent(IItem item, XElement div, bool hasWords)
        {
            if (IncludeComments) div.Add(new XComment("item " + item.Id));

            TiledTextPart part = item.Parts.Find(p => p.TypeId == _tiledTextPartTypeId)
                as TiledTextPart;

            if (part == null)
            {
                Logger?.LogError($"Item {item.Id} has no text part");
                return;
            }

            foreach (TextTileRow row in part.Rows)
            {
                string name = row.Data.ContainsKey(XmlTextParser.KEY_NAME) ?
                    row.Data[XmlTextParser.KEY_NAME] : "l";
                XElement lp = new XElement(XmlHelper.TEI + name);

                foreach (var pair in row.Data)
                    lp.Add(GetAttribute(pair.Key, pair.Value));

                if (hasWords)
                {
                    foreach (TextTile tile in row.Tiles)
                    {
                        XElement w = new XElement(XmlHelper.TEI + "w",
                            GetTileText(tile));
                        foreach (var pair in tile.Data)
                        {
                            XAttribute attr = GetAttribute(pair.Key, pair.Value);
                            if (attr != null) w.Add(attr);
                        }
                        lp.Add(w);
                    }
                }
                else
                {
                    lp.Value = string.Join(" ", from t in row.Tiles
                        select GetTileText(t));
                }
                div.Add(lp);
            }
        }

        public async Task ExportAsync(string outputDir,
            IProgress<ProgressReport> progress)
        {
            if (outputDir is null)
                throw new ArgumentNullException(nameof(outputDir));

            PagingOptions groupFilter = new PagingOptions
            {
                PageNumber = 1,
                PageSize = 100
            };
            DataPage<string> groupPage =
                await Repository.GetDistinctGroupIdsAsync(groupFilter);
            Logger?.LogInformation($"Groups found: {groupPage.Total}");

            ProgressReport report = progress != null? new ProgressReport() : null;

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

                    // open the target document: it is assumed that the target
                    // files are under their author subdirectory, which is
                    // the part of the group ID before the first dash; e.g.
                    // ABLAB-epig is under a subdirectory named ABLAB
                    string authDirName = GetGroupIdDirectoryName(groupId);
                    string filePath = authDirName == null
                        ? Path.Combine(outputDir, groupId + ".xml")
                        : Path.Combine(outputDir, authDirName, groupId + ".xml");

                    if (!File.Exists(filePath))
                    {
                        Logger?.LogError($"Target file not exists: {filePath}");
                        continue;
                    }

                    XDocument doc = XDocument.Load(filePath,
                        // LoadOptions.PreserveWhitespace |
                        LoadOptions.SetLineInfo);
                    XElement body = XmlHelper.GetTeiBody(doc);

                    // determine if we should output w elements
                    bool hasWordElems = await HasWordGranularityAsync(doc, groupId);

                    // clear the div children except for initial head
                    foreach (XElement div in body.Descendants(XmlHelper.TEI + "div2"))
                        ClearDivContents(div);
                    foreach (XElement div in body.Descendants(XmlHelper.TEI + "div1"))
                        ClearDivContents(div);

                    // for each item in the group, sorted by key:
                    itemFilter.GroupId = groupId;
                    itemFilter.PageNumber = 1;
                    DataPage<ItemInfo> itemPage = Repository.GetItems(itemFilter);

                    while (itemFilter.PageNumber <= itemPage.PageCount)
                    {
                        foreach (ItemInfo info in itemPage.Items)
                        {
                            int i = info.Title.LastIndexOf('#');
                            Debug.Assert(i > -1);
                            string id = info.Title.Substring(i + 1);

                            // locate the target div element via its @xml:id
                            // (log error and continue if not found)
                            XElement div = body.Descendants()
                                .FirstOrDefault(e => (e.Name.LocalName == "div1"
                                    || e.Name.LocalName == "div2")
                                    && e.Attribute(XmlHelper.XML + "id")?.Value == id);
                            if (div == null)
                            {
                                Logger?.LogError(
                                    $"Target div for item ID {info.Id} not found");
                                continue;
                            }

                            // get the item
                            IItem item = Repository.GetItem(info.Id);

                            // build and append content elements
                            AppendItemContent(item, div, hasWordElems);
                        }
                        if (++itemFilter.PageNumber <= itemPage.PageCount)
                            itemPage = Repository.GetItems(itemFilter);
                    }

                    doc.Save(filePath, SaveOptions.OmitDuplicateNamespaces);

                    if (progress != null)
                    {
                        report.Count++;
                        report.Percent = report.Count * 100 / groupPage.Total;
                        report.Message = groupId;
                        progress.Report(report);
                    }
                } // group

                // next groups page
                if (++groupFilter.PageNumber <= groupPage.PageCount)
                    groupPage = await Repository.GetDistinctGroupIdsAsync(groupFilter);
            }
            if (progress != null)
            {
                report.Count = groupPage.Total;
                report.Percent = 100;
                progress.Report(report);
            }
        }
    }
}
