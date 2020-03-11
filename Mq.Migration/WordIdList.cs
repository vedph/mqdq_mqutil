using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// A list of word IDs in their order, as they appear in a MQDQ text
    /// XML document.
    /// </summary>
    public sealed class WordIdList : IHasLogger
    {
        /// <summary>
        /// Gets the IDs.
        /// </summary>
        public IList<Tuple<string, string>> IdAndWords { get; }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WordIdList"/> class.
        /// </summary>
        public WordIdList()
        {
            IdAndWords = new List<Tuple<string, string>>();
        }

        /// <summary>
        /// Parses the specified document, collecting all the word IDs in
        /// their order.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <exception cref="ArgumentNullException">doc</exception>
        public void Parse(XDocument doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            IdAndWords.Clear();
            foreach (XElement wElem in XmlHelper.GetTeiBody(doc)
                .Descendants(XmlHelper.TEI + "w"))
            {
                IdAndWords.Add(Tuple.Create(
                    wElem.Attribute(XmlHelper.XML + "id").Value,
                    wElem.Value.Trim()));
            }
        }

        /// <summary>
        /// Gets all the IDs in the specified range.
        /// </summary>
        /// <param name="from">From ID (included).</param>
        /// <param name="to">To ID (included).</param>
        /// <returns>Range of tuples where 1=ID and 2=word, or null if
        /// <paramref name="from"/> was not found.</returns>
        /// <exception cref="ArgumentNullException">from or to</exception>
        public IList<Tuple<string, string>> GetRange(string from, string to)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));

            // find @from (error if not found)
            Tuple<string, string> first = IdAndWords
                .FirstOrDefault(iw => iw.Item1 == from);
            if (first == null)
            {
                Logger?.LogError($"From-ID not found: {from}");
                return null;
            }
            int firstIndex = IdAndWords.IndexOf(first);

            // find @to (error if not found)
            Tuple<string, string> last = IdAndWords
                .Skip(firstIndex)
                .FirstOrDefault(iw => iw.Item1 == to);
            if (last == null)
            {
                last = IdAndWords.FirstOrDefault(iw => iw.Item1 == to);
            }
            if (last == null)
            {
                Logger?.LogError($"To-ID not found: {to}");
                return null;
            }
            int lastIndex = IdAndWords.IndexOf(last);

            if (firstIndex == lastIndex)
            {
                return new List<Tuple<string, string>>
                {
                    first
                };
            }

            // check for inverted @from/@to
            if (lastIndex < firstIndex)
            {
                Logger?.LogWarning($"Inverted range: from={from}-to={to}");
                int i = firstIndex;
                firstIndex = lastIndex;
                lastIndex = i;
            }

            return IdAndWords
                .Skip(firstIndex)
                .Take(lastIndex + 1 - firstIndex)
                .ToList();
        }
    }
}
