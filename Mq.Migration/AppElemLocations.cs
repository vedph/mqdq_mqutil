using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// An app element with its locations. This is used when dealing with
    /// overlaps.
    /// </summary>
    public sealed class AppElemLocations
    {
        /// <summary>
        /// Gets the app element.
        /// </summary>
        public XElement Element { get; }

        /// <summary>
        /// Gets the locations.
        /// </summary>
        public Tuple<string, string>[] Locations { get; }

        /// <summary>
        /// Gets the app element line number.
        /// </summary>
        public int LineNumber => ((IXmlLineInfo)Element)?.LineNumber ?? 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppElemLocations"/> class.
        /// </summary>
        /// <param name="appElem">The app element.</param>
        /// <param name="locations">The locations.</param>
        public AppElemLocations(XElement appElem,
            IEnumerable<Tuple<string, string>> locations)
        {
            Element = appElem;
            Locations = locations.ToArray();
        }

        /// <summary>
        /// Check if this app element overlaps the other one.
        /// </summary>
        /// <param name="other">The other app element.</param>
        /// <returns>True if overlap; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">other</exception>
        public bool Overlaps(AppElemLocations other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            return Locations.Any(id => Array.IndexOf(other.Locations, id) > -1);
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Join(" ", Locations.Select(t => $"{t.Item1}={t.Item2}"));
        }
    }
}
