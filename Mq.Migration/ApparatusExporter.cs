using Cadmus.Core;
using Cadmus.Core.Storage;
using Cadmus.Parts.Layers;
using Cadmus.Philology.Parts.Layers;
using Fusi.Tools;
using Fusi.Tools.Config;
using Fusi.Tools.Data;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// Apparatus exporter.
    /// </summary>
    /// <seealso cref="IHasLogger" />
    public sealed class ApparatusExporter : ExporterBase
    {
        private readonly string _layerPartTypeId;
        private readonly string[] _apparatusFrIds;
        private readonly NoteElementRenderer _noteRenderer;
        private readonly LocationToIdMapper _locMapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApparatusExporter"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <exception cref="ArgumentNullException">repository</exception>
        public ApparatusExporter(ICadmusRepository repository) : base(repository)
        {
            _layerPartTypeId =
                new TiledTextLayerPart<ApparatusLayerFragment>().TypeId;

            TagAttribute attr = typeof(ApparatusLayerFragment).GetTypeInfo()
                .GetCustomAttribute<TagAttribute>();
            string frTag = attr != null ? attr.Tag : GetType().FullName;
            _apparatusFrIds = new string[]
            {
                frTag,
                $"{frTag}:ancient",
                $"{frTag}:margin"
            };
            _noteRenderer = new NoteElementRenderer();
            _locMapper = new LocationToIdMapper();
        }

        private TiledTextLayerPart<ApparatusLayerFragment> GetApparatusPart(
            List<IPart> parts, string roleId)
        {
            return parts.Find(p => p.TypeId == _layerPartTypeId
                              && p.RoleId == roleId)
                as TiledTextLayerPart<ApparatusLayerFragment>;
        }

        private string RenderEntrySources(IList<ApparatusAnnotatedValue> sources,
            XElement target)
        {
            StringBuilder sb = new StringBuilder();
            foreach (ApparatusAnnotatedValue source in sources)
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(source.Value);

                if (!string.IsNullOrEmpty(source.Note))
                    target.Add(_noteRenderer.Render(source.Note, $"#{source.Value}"));
            }
            return sb.ToString();
        }

        private void AppendEntryContent(ApparatusEntry entry, XElement app)
        {
            XElement target = null;

            // only replacement/note are allowed for TEI
            switch (entry.Type)
            {
                case ApparatusEntryType.Replacement:
                    // replacement = lem/rdg with value
                    target = new XElement(XmlHelper.TEI +
                        (entry.IsAccepted ? "lem" : "rdg"),
                        entry.Value);
                    // groupId = @n (temporary solution)
                    // TODO: decide an attribute
                    if (!string.IsNullOrEmpty(entry.GroupId))
                        target.SetAttributeValue("n", entry.GroupId);
                    app.Add(target);
                    break;

                case ApparatusEntryType.Note:
                    // note = add/note
                    app.Add(_noteRenderer.Render(entry.Note));
                    break;

                default:
                    Logger?.LogError($"Invalid apparatus entry type: {entry.Type}");
                    return;
            }

            // normValue = ident's with optional @n
            // only lem/rdg should have ident's (??)
            if (!string.IsNullOrEmpty(entry.NormValue))
            {
                if (target == null)
                {
                    Logger?.LogError(
                        $"NormValue in non-replacement entry: \"{entry.NormValue}\"");
                }
                else
                {
                    foreach (string token in entry.NormValue.Split())
                    {
                        if (token.Length == 0) continue;

                        // ident [@n]
                        XElement ident = new XElement(XmlHelper.TEI + "ident");
                        int i = token.IndexOf('#');
                        if (i > -1)
                        {
                            ident.Value = token.Substring(0, i);
                            ident.SetAttributeValue("n", token.Substring(i + 1));
                        }
                        else ident.Value = token;
                        target.Add(ident);
                    }
                }
            }

            // witnesses
            if (entry.Witnesses?.Count > 0)
                RenderEntrySources(entry.Witnesses, target ?? app);

            // authors
            if (entry.Authors?.Count > 0)
                RenderEntrySources(entry.Authors, target ?? app);
        }

        private void AppendApparatusContent(
            string roleId,
            IItem item,
            TiledTextLayerPart<ApparatusLayerFragment> part,
            XElement div)
        {
            if (IncludeComments)
                div.Add(new XComment($"apparatus {roleId} ({part.Fragments.Count})"));

            // each fragment is an app element
            foreach (ApparatusLayerFragment fr in part.Fragments)
            {
                // nope if no entries
                if (fr.Entries.Count == 0) continue;

                XElement app = new XElement(XmlHelper.TEI + "app");

                // location
                var ft = _locMapper.Map(fr.Location, item);
                if (ft != null)
                {
                    if (ft.Item2 != null)
                    {
                        app.SetAttributeValue("from", "#" + ft.Item1);
                        app.SetAttributeValue("to", "#" + ft.Item2);
                    }
                    else
                    {
                        string target = "#" + ft.Item1;
                        app.SetAttributeValue("from", target);
                        app.SetAttributeValue("to", target);
                    }
                }

                // tag 2nd token = @type
                if (!string.IsNullOrEmpty(fr.Tag))
                {
                    int i = fr.Tag.IndexOf(' ');
                    if (i > -1) app.SetAttributeValue("type", fr.Tag.Substring(i + 1));
                }
                div.Add(app);

                // add fragment's entries
                foreach (ApparatusEntry entry in fr.Entries)
                    AppendEntryContent(entry, app);
            }
        }

        private bool AppendItemContent(IItem item, XElement div)
        {
            if (IncludeComments) div.Add(new XComment("item " + item.Id));

            int count = 0;
            foreach (string roleId in _apparatusFrIds)
            {
                TiledTextLayerPart<ApparatusLayerFragment> part =
                    GetApparatusPart(item.Parts, roleId);
                if (part == null)
                {
                    Logger?.LogInformation($"Item {item.Id} has no {roleId} part");
                }
                else
                {
                    AppendApparatusContent(roleId, item, part, div);
                    count++;
                }
            }
            if (count == 0)
                Logger?.LogError($"Item {item.Id} has no apparatus part");
            return count > 0;
        }

        /// <summary>
        /// Exports the apparatus documents.
        /// </summary>
        /// <param name="outputDir">The output directory.</param>
        /// <param name="progress">The optional progress reporter.</param>
        /// <exception cref="ArgumentNullException">outputDir</exception>
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

            ProgressReport report = progress != null ? new ProgressReport
            {
                Count = 0
            } : null;

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

                    // clear the div1 children except for initial head
                    foreach (XElement div in body.Descendants(XmlHelper.TEI + "div1"))
                        ClearDivContents(div);

                    // for each item in the group, sorted by key:
                    itemFilter.GroupId = groupId;
                    itemFilter.PageNumber = 1;
                    DataPage<ItemInfo> itemPage = Repository.GetItems(itemFilter);
                    bool anyApparatus = false;

                    while (itemFilter.PageNumber <= itemPage.PageCount)
                    {
                        foreach (ItemInfo info in itemPage.Items)
                        {
                            // get the item and its parts
                            IItem item = Repository.GetItem(info.Id);

                            // locate the target div1 element via its @xml:id
                            // (log error and continue if not found)
                            int i = info.Title.LastIndexOf('#');
                            Debug.Assert(i > -1);
                            string id = info.Title.Substring(i + 1);

                            XElement div = body.Descendants()
                                .FirstOrDefault(e => e.Name.LocalName == "div1"
                                    && e.Attribute(XmlHelper.XML + "id")?.Value == id);
                            if (div == null)
                            {
                                Logger?.LogError(
                                    $"Target div for item ID {info.Id} not found");
                                continue;
                            }

                            // build and append content elements
                            if (AppendItemContent(item, div)) anyApparatus = true;
                        }
                        if (++itemFilter.PageNumber <= itemPage.PageCount)
                            itemPage = Repository.GetItems(itemFilter);
                    }

                    // if no apparatus was found, don't touch the original document
                    if (anyApparatus)
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
