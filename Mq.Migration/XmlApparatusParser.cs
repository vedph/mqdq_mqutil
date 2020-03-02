using Cadmus.Parts.Layers;
using Cadmus.Philology.Parts.Layers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// XML apparatus document parser.
    /// </summary>
    public sealed class XmlApparatusParser
    {
        public IEnumerable<TiledTextLayerPart<ApparatusLayerFragment>> Parse(
            XDocument doc, string id, JsonTextIndex textIndex)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (textIndex == null) throw new ArgumentNullException(nameof(textIndex));

            // TODO
            throw new NotImplementedException();
        }
    }
}
