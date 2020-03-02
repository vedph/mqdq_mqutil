using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Mq.Migration
{
    /// <summary>
    /// Index for a dumped JSON text as output by <see cref="XmlTextParser"/>.
    /// The index contains a key for each tile ID, mapped to the corresponding
    /// item ID and Y,X coordinates pair.
    /// </summary>
    public sealed class JsonTextIndex
    {
        private readonly Dictionary<string, JsonTextIndexPayload> _index;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonTextIndex"/> class.
        /// </summary>
        public JsonTextIndex()
        {
            _index = new Dictionary<string, JsonTextIndexPayload>();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear() => _index.Clear();

        /// <summary>
        /// Index data from the specified stream, adding them to this index.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <exception cref="ArgumentNullException">stream</exception>
        public void Index(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using (JsonDocument doc = JsonDocument.Parse(stream, new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            }))
            {
                // item
                foreach (JsonElement itemElem in doc.RootElement.EnumerateArray())
                {
                    // item/id
                    string itemId = itemElem.GetProperty("id").GetString();
                    // item/parts
                    JsonElement partElem =
                        itemElem.GetProperty("parts").EnumerateArray().First();
                    // item/parts[0]/rows
                    int y = 0;
                    foreach (JsonElement rowElem in partElem.GetProperty("rows")
                        .EnumerateArray())
                    {
                        y++;
                        int x = 0;
                        foreach (JsonElement tileElem in
                            rowElem.GetProperty("tiles").EnumerateArray())
                        {
                            x++;
                            string tileId = tileElem
                                .GetProperty("data")
                                .GetProperty("id").GetString();
                            _index[tileId] = new JsonTextIndexPayload(itemId, y, x);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds the specified tile identifier in this index.
        /// </summary>
        /// <param name="tileId">The tile identifier.</param>
        /// <returns>The corresponding payload or null if not found.</returns>
        public JsonTextIndexPayload Find(string tileId)
        {
            return _index.ContainsKey(tileId)? _index[tileId] : null;
        }
    }

    /// <summary>
    /// Payload of a <see cref="JsonTextIndex"/>.
    /// </summary>
    public sealed class JsonTextIndexPayload
    {
        /// <summary>
        /// Gets the item identifier.
        /// </summary>
        public string ItemId { get; }

        /// <summary>
        /// Gets the Y coordinate.
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Gets the X coordinate.
        /// </summary>
        public int X { get; }

        public JsonTextIndexPayload(string itemId, int y, int x)
        {
            ItemId = itemId;
            Y = y;
            X = x;
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{ItemId} {Y}.{X}";
        }
    }
}
