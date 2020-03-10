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
    public sealed class WordIdList
    {
        /// <summary>
        /// Gets the IDs.
        /// </summary>
        public IList<Tuple<string, string>> IdAndWords { get; }

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

            Tuple<string, string> first = IdAndWords
                .FirstOrDefault(iw => iw.Item1 == from);
            if (first == null) return null;
            int i = IdAndWords.IndexOf(first);

            List<Tuple<string, string>> rangeIds =
                new List<Tuple<string, string>>();
            while (i < IdAndWords.Count)
            {
                rangeIds.Add(IdAndWords[i]);
                if (IdAndWords[i].Item1 == to) break;
                i++;
            }
            return rangeIds;
        }
    }
}
