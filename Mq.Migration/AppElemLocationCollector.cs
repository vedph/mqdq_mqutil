using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// App elements locations collector.
    /// </summary>
    public static class AppElemLocationCollector
    {
        /// <summary>
        /// Determines whether the specified app element is subject to overlap.
        /// </summary>
        /// <param name="app">The app element to test.</param>
        /// <returns>
        /// <c>true</c> if overlappable; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">app</exception>
        public static bool IsOverlappable(XElement app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            // app with type=margin-note is on another layer
            if (app.Attribute("type")?.Value == "margin-note") return false;

            // overlaps are possible when any of the lem/rdg is not ancient-note
            var children = app.Elements()
                .Where(e => e.Name.LocalName == "lem"
                       || e.Name.LocalName == "rdg");

            return children.Any(e => e.Attribute("type") == null
                || e.Attribute("type").Value != "ancient-note");
        }

        /// <summary>
        /// Collects the app elements locations from the specified XML document.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="widList">The word IDs list for the document.</param>
        /// <param name="appElemFilter">The optional app element filter.</param>
        /// <returns>Collected app element locations.</returns>
        /// <exception cref="ArgumentNullException">doc or widList</exception>
        public static List<AppElemLocations> Collect(
            XDocument doc, WordIdList widList, Func<XElement, bool> appElemFilter)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));
            if (widList == null)
                throw new ArgumentNullException(nameof(widList));

            // collect overlappable app locations
            char[] wsSeps = new[] { ' ' };
            List<AppElemLocations> appWithSets = new List<AppElemLocations>();

            foreach (XElement appElem in XmlHelper.GetTeiBody(doc)
                .Descendants(XmlHelper.TEI + "app")
                .Where(app => appElemFilter == null || appElemFilter(app)))
            {
                Tuple<string, string>[] ids;
                if (appElem.Attribute("loc") != null)
                {
                    ids = (from rid in appElem.Attribute("loc").Value
                            .Split(wsSeps, StringSplitOptions.RemoveEmptyEntries)
                           let id = rid.Substring(1)
                           let iw = widList.IdAndWords.FirstOrDefault(t => t.Item1 == id)
                           where iw != null
                           select iw).ToArray();
                }
                else
                {
                    ids = widList.GetRange(
                        appElem.Attribute("from").Value.Substring(1),
                        appElem.Attribute("to").Value.Substring(1))?.ToArray();
                }
                if (ids == null || ids.Length == 0) continue;  // should not happen

                appWithSets.Add(new AppElemLocations(appElem, ids));
            }
            return appWithSets;
        }
    }
}
