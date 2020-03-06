using System;
using System.IO;
using Cadmus.Core;
using Cadmus.Core.Config;
using Cadmus.Core.Storage;
using Cadmus.Mongo;
using Microsoft.Extensions.Configuration;

namespace Mqutil.Services
{
    internal sealed class RepositoryService
    {
        private readonly IConfiguration _configuration;

        public RepositoryService(IConfiguration configuration)
        {
            _configuration = configuration ??
                throw new ArgumentNullException(nameof(configuration));
        }

        public ICadmusRepository CreateRepository(string database)
        {
            if (database == null) throw new ArgumentNullException(nameof(database));

            // build the tags to types map
            string pluginDir = Path.Combine(
                Directory.GetCurrentDirectory(), "Plugins");
            TagAttributeToTypeMap map = new TagAttributeToTypeMap();
            map.Add(pluginDir,
                "*parts*.dll",
                new PluginLoadContext(pluginDir));

            // create the repository (no need to use container here)
            MongoCadmusRepository repository = new MongoCadmusRepository(
                new StandardPartTypeProvider(map),
                new StandardItemSortKeyBuilder());
            repository.Configure(new MongoCadmusRepositoryOptions
            {
                ConnectionString = string.Format(
                    _configuration.GetConnectionString("Mongo"),
                    database)
            });

            return repository;
        }
    }
}
