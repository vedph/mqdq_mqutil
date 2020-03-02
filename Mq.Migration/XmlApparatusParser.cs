using Cadmus.Parts.Layers;
using Cadmus.Philology.Parts.Layers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// XML apparatus document parser.
    /// </summary>
    public sealed class XmlApparatusParser : IHasLogger
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Parses the specified document.
        /// </summary>
        /// <param name="doc">The document.</param>
        /// <param name="id">The document identifier.</param>
        /// <param name="textIndex">The index of the corresponding text.</param>
        /// <returns>Apparatus layer parts.</returns>
        /// <exception cref="ArgumentNullException">doc or id or textIndex
        /// </exception>
        /// <exception cref="NotImplementedException"></exception>
        public IEnumerable<TiledTextLayerPart<ApparatusLayerFragment>> Parse(
            XDocument doc, string id, JsonTextIndex textIndex)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (textIndex == null) throw new ArgumentNullException(nameof(textIndex));

            XElement divElem = doc.Root
                .Element(XmlHelper.TEI + "text")
                .Element(XmlHelper.TEI + "body")
                .Element(XmlHelper.TEI + "div1");

            foreach (XElement appElem in divElem.Elements(XmlHelper.TEI + "app"))
            {
                // TODO
            }
            throw new NotImplementedException();
        }
    }
}
