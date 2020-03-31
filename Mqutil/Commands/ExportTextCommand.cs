using Cadmus.Core.Storage;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mq.Migration;
using Mqutil.Services;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Mqutil.Commands
{
    public sealed class ExportTextCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly string _outputDir;
        private readonly string _database;
        private readonly RepositoryService _repositoryService;

        public ExportTextCommand(AppOptions options,
            string database, string outputDir)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            _outputDir = outputDir
                ?? throw new ArgumentNullException(nameof(outputDir));
            _database = database
                ?? throw new ArgumentNullException(nameof(database));

            _config = options.Configuration;
            _repositoryService = new RepositoryService(_config);
        }

        /// <summary>
        /// Configures the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException">command</exception>
        public static void Configure(CommandLineApplication command,
            AppOptions options)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            command.Description = "Export MQDQ database to TEI text documents";
            command.HelpOption("-?|-h|--help");

            CommandArgument databaseArgument = command.Argument("[database]",
                "The database name");
            CommandArgument outputDirArgument = command.Argument("[output-dir]",
                "The output directory with target TEI documents");

            command.OnExecute(() =>
            {
                options.Command = new ExportTextCommand(
                    options,
                    databaseArgument.Value,
                    outputDirArgument.Value);
                return 0;
            });
        }

        public async Task Run()
        {
            Console.WriteLine("EXPORT TEXT INTO TEI FILES\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Database: {_database}\n" +
                $"Output dir: {_outputDir}\n");

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(Log.Logger);
            Log.Logger.Information("EXPORT TEXT INTO TEI FILES");

            ICadmusRepository repository =
                _repositoryService.CreateRepository(_database);

            TextExporter exporter = new TextExporter(repository);
            await exporter.ExportAsync(_outputDir);
        }
    }
}
