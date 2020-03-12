using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Mq.Migration;
using Mqutil.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mqutil.Commands
{
    public sealed class RemoveOverlapsCommand : ICommand
    {
        private readonly string _appFileDir;
        private readonly string _appFileMask;
        private readonly string _outputDir;
        private readonly bool _regexMask;
        private readonly bool _recursive;

        public RemoveOverlapsCommand(string appFileDir,
            string appFileMask, string outputDir, bool regexMask,
            bool recursive)
        {
            _appFileDir = appFileDir ??
                throw new ArgumentNullException(nameof(appFileDir));
            _appFileMask = appFileMask ??
                throw new ArgumentNullException(nameof(appFileMask));
            _outputDir = outputDir ??
                throw new ArgumentNullException(nameof(outputDir));
            _regexMask = regexMask;
            _recursive = recursive;
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

            command.Description = "Remove app overlaps from text documents " +
                "saving the updated documents into the specified directory.";
            command.HelpOption("-?|-h|--help");

            CommandArgument appDirArgument = command.Argument("[app-file-dir]",
                "The input entries files directory");
            CommandArgument appMaskArgument = command.Argument("[app-file-mask]",
                "The input entries files mask");
            CommandArgument outputDirArgument = command.Argument("[output-dir]",
                "The output directory");

            CommandOption regexMaskOption = command.Option("-r|--regex",
                "Use regular expressions in files masks", CommandOptionType.NoValue);
            CommandOption recursiveOption = command.Option("-s|--sub",
                "Recurse subdirectories in matching files masks",
                CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                options.Command = new ReportOverlapsCommand(
                    appDirArgument.Value,
                    appMaskArgument.Value,
                    outputDirArgument.Value,
                    regexMaskOption.HasValue(),
                    recursiveOption.HasValue());
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("REMOVE OVERLAPS\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Input:  {_appFileMask}\n" +
                $"Output: {_outputDir}\n");

            int inputFileCount = 0;
            int overlapCount = 0;

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(Log.Logger);

            // for each app document
            WordIdList widList = new WordIdList
            {
                Logger = loggerFactory.CreateLogger("report-overlaps")
            };
            foreach (string filePath in FileEnumerator.Enumerate(
                _appFileDir, _appFileMask, _regexMask, _recursive))
            {
                Console.WriteLine();
                Log.Logger.Information("Parsing {FilePath}", filePath);

                // load app document
                string inputFileName = Path.GetFileNameWithoutExtension(filePath);
                Console.WriteLine(filePath);
                inputFileCount++;
                XDocument doc = XDocument.Load(filePath,
                    LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

                // collect word IDs from text document
                widList.Parse(XDocument.Load(filePath.Replace("-app.", ".")));

                // collect app's locations
                List<AppElemLocations> appWithLocs =
                    AppElemLocationCollector.Collect(doc, widList,
                    AppElemLocationCollector.IsOverlappable);

                // detect and report overlaps
                for (int i = 0; i < appWithLocs.Count - 1; i++)
                {
                    for (int j = i + 1; j < appWithLocs.Count; j++)
                    {
                        if (appWithLocs[i].Overlaps(appWithLocs[j]))
                        {
                            overlapCount++;
                            // TODO
                            goto nextOuter;
                        }
                    }
                nextOuter:
                    if (i % 10 == 0) Console.Write('.');
                }
            }

            // TODO
            Console.WriteLine($"\nInput documents: {inputFileCount}");
            return Task.CompletedTask;
        }
    }
}
