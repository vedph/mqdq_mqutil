using Cadmus.Core;
using Cadmus.Core.Layers;
using Cadmus.Parts.General;
using Microsoft.Extensions.Logging;
using System;

namespace Mq.Migration
{
    /// <summary>
    /// Fragment's location to TEI xml:id attribute mapper.
    /// This is used when exporting apparatus.
    /// </summary>
    public sealed class LocationToIdMapper : IHasLogger
    {
        private readonly string _tiledTextPartTypeId;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationToIdMapper"/>
        /// class.
        /// </summary>
        public LocationToIdMapper()
        {
            _tiledTextPartTypeId = new TiledTextPart().TypeId;
        }

        private string MapPoint(TokenTextPoint point, TiledTextPart part)
        {
            int rowIndex = point.Y - 1;
            int tileIndex = point.X - 1;

            if (rowIndex >= part.Rows.Count)
            {
                Logger?.LogError($"Location point {point} out of part's rows");
                return null;
            }

            if (tileIndex >= part.Rows[rowIndex].Tiles.Count)
            {
                Logger?.LogError($"Location point {point} out of part's row tiles");
                return null;
            }

            if (!part.Rows[rowIndex].Tiles[tileIndex].Data.ContainsKey("id"))
            {
                Logger?.LogError($"Location point {point} maps to tile without id");
                return null;
            }

            return part.Rows[rowIndex].Tiles[tileIndex].Data["id"];
        }

        /// <summary>
        /// Maps the specified location to 1 or 2 (for ranges) IDs.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="item">The parent item. The tiled text part will be
        /// searched among its parts.</param>
        /// <returns>Tuple where 1=first or single ID, 2=second ID or null
        /// when there is a single ID only; null if text part not found.</returns>
        /// <exception cref="ArgumentNullException">location or item</exception>
        public Tuple<string, string> Map(string location, IItem item)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            // find the tiled text part
            TiledTextPart part =
                item.Parts.Find(p => p.TypeId == _tiledTextPartTypeId)
                as TiledTextPart;
            if (part == null)
            {
                Logger?.LogError($"No tiled text part for item {item}");
                return null;
            }

            // parse and map location
            TokenTextLocation loc = TokenTextLocation.Parse(location);
            string a = MapPoint(loc.A, part);
            if (a == null) return null;

            if (loc.IsRange)
            {
                string b = MapPoint(loc.B, part);
                if (b == null) return null;
                return Tuple.Create(a, b);
            }
            else
            {
                return Tuple.Create(a, (string)null);
            }
        }
    }
}
