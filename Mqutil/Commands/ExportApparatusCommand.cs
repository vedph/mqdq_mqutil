using Cadmus.Core;
using Cadmus.Core.Storage;
using Fusi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mq.Migration;
using Mqutil.Services;
using Serilog;
using ShellProgressBar;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Mqutil.Commands
{
    public sealed class ExportApparatusCommand : ICommand
    {
        private readonly IConfiguration _config;
        private readonly string _outputDir;
        private readonly string _database;
        private readonly bool _comments;
        private readonly bool _dry;
        private readonly IRepositoryProvider _repositoryProvider;

        public ExportApparatusCommand(AppOptions options,
            string database, string outputDir, bool comments, bool dry)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            _outputDir = outputDir
                ?? throw new ArgumentNullException(nameof(outputDir));
            _database = database
                ?? throw new ArgumentNullException(nameof(database));
            _comments = comments;
            _dry = dry;

            _config = options.Configuration;
            _repositoryProvider = new StandardRepositoryProvider(_config);
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

            command.Description = "Export MQDQ database to TEI apparatus documents";
            command.HelpOption("-?|-h|--help");

            CommandArgument databaseArgument = command.Argument("[database]",
                "The database name");
            CommandArgument outputDirArgument = command.Argument("[output-dir]",
                "The output directory with target TEI documents");
            CommandOption commentOption = command.Option("-c|--comments",
                "Include comments in output", CommandOptionType.NoValue);
            CommandOption dryOption = command.Option("-d|--dry",
                "Dry run: do not write output", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                options.Command = new ExportApparatusCommand(
                    options,
                    databaseArgument.Value,
                    outputDirArgument.Value,
                    commentOption.HasValue(),
                    dryOption.HasValue());
                return 0;
            });
        }

        public async Task Run()
        {
            Console.WriteLine("EXPORT APPARATUS INTO TEI FILES\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Database: {_database}\n" +
                $"Output dir: {_outputDir}\n" +
                $"Comments: {_comments}\n" +
                $"Dry: {_dry}\n");

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(Log.Logger);
            Log.Logger.Information("EXPORT APPARATUS INTO TEI FILES");

            ICadmusRepository repository =
                _repositoryProvider.CreateRepository(_database);

            ApparatusExporter exporter = new ApparatusExporter(repository)
            {
                Logger = loggerFactory.CreateLogger("export"),
                IncludeComments = _comments,
                IsDryModeEnabled = _dry
            };
            if (!Directory.Exists(_outputDir))
                Directory.CreateDirectory(_outputDir);

            using (var bar = new ProgressBar(100, "Exporting...",
                new ProgressBarOptions
                {
                    ProgressCharacter = '.',
                    ProgressBarOnBottom = true,
                    DisplayTimeInRealTime = false
                }))
            {
                await exporter.ExportAsync(_outputDir,
                    new Progress<ProgressReport>(
                        r => bar.Tick(r.Percent, r.Message)));
            }
        }
    }
}
