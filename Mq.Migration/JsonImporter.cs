using Cadmus.Core;
using Cadmus.Core.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;

namespace Mq.Migration
{
    /// <summary>
    /// JSON text and apparatus dumps importer.
    /// </summary>
    /// <seealso cref="Mq.Migration.IHasLogger" />
    public sealed class JsonImporter : IHasLogger
    {
        private readonly JsonDocumentOptions _options;
        private readonly ICadmusRepository _repository;

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonImporter"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <exception cref="ArgumentNullException">repository</exception>
        public JsonImporter(ICadmusRepository repository)
        {
            _options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true
            };
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
        }

        private static IItem ReadItem(JsonElement itemElem)
        {
            return new Item
            {
                Id = itemElem.GetProperty("id").GetString(),
                Title = itemElem.GetProperty("title").GetString(),
                Description = itemElem.GetProperty("description").GetString(),
                FacetId = itemElem.GetProperty("facetId").GetString(),
                GroupId = itemElem.GetProperty("groupId").GetString(),
                SortKey = itemElem.GetProperty("sortKey").GetString(),
                TimeCreated = itemElem.GetProperty("timeCreated").GetDateTime(),
                CreatorId = itemElem.GetProperty("creatorId").GetString(),
                TimeModified = itemElem.GetProperty("timeModified").GetDateTime(),
                UserId = itemElem.GetProperty("userId").GetString(),
                Flags = itemElem.GetProperty("flags").GetInt32()
            };
        }

        /// <summary>
        /// Imports the specified text stream.
        /// </summary>
        /// <param name="txtStream">The text stream.</param>
        /// <param name="appStream">The application stream.</param>
        /// <exception cref="ArgumentNullException">txtStream or appStream</exception>
        public void Import(Stream txtStream, Stream appStream)
        {
            if (txtStream == null) throw new ArgumentNullException(nameof(txtStream));
            if (appStream == null) throw new ArgumentNullException(nameof(appStream));

            JsonDocument txtDoc = JsonDocument.Parse(txtStream, _options);
            JsonDocument appDoc = JsonDocument.Parse(appStream, _options);

            // for each item
            foreach (JsonElement itemElem in txtDoc.RootElement.EnumerateArray())
            {
                // read its metadata
                IItem item = ReadItem(itemElem);

                // import it
                Logger?.LogInformation("Importing item {ItemId}: {Title}",
                    item.Id, item.Title);
                _repository.AddItem(item);

                // import its parts
                foreach (JsonElement partElem in itemElem.GetProperty("parts")
                    .EnumerateArray())
                {
                    _repository.AddPartFromContent(partElem.ToString());
                }
            }

            // apparatus
            int n = 0;
            foreach (JsonElement partElem in appDoc.RootElement.EnumerateArray())
            {
                Logger?.LogInformation($"Importing layer part #{++n}");
                _repository.AddPartFromContent(partElem.ToString());
            }
        }
    }
}
