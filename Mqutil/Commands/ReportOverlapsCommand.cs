using Microsoft.Extensions.CommandLineUtils;
using Mq.Migration;
using Serilog;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Mqutil.Services;
using System.Xml.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Mqutil.Commands
{
    public sealed class ReportOverlapsCommand : ICommand
    {
        private readonly string _appFileDir;
        private readonly string _appFileMask;
        private readonly string _outputPath;
        private readonly bool _regexMask;
        private readonly bool _recursive;

        public ReportOverlapsCommand(string appFileDir,
            string appFileMask, string outputPath, bool regexMask,
            bool recursive)
        {
            _appFileDir = appFileDir ??
                throw new ArgumentNullException(nameof(appFileDir));
            _appFileMask = appFileMask ??
                throw new ArgumentNullException(nameof(appFileMask));
            _outputPath = outputPath ??
                throw new ArgumentNullException(nameof(outputPath));
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

            command.Description = "Parse the MQDQ apparatus and text documents " +
                "creating an overlaps report into the specified file";
            command.HelpOption("-?|-h|--help");

            CommandArgument appDirArgument = command.Argument("[app-file-dir]",
                "The input entries files directory");
            CommandArgument appMaskArgument = command.Argument("[app-file-mask]",
                "The input entries files mask");
            CommandArgument outputPathArgument = command.Argument("[output-path]",
                "The output file path");

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
                    outputPathArgument.Value,
                    regexMaskOption.HasValue(),
                    recursiveOption.HasValue());
                return 0;
            });
        }

        private bool IsOverlappable(XElement app)
        {
            // app with type=margin-note is on another layer
            if (app.Attribute("type")?.Value == "margin-note") return false;

            // overlaps are possible when any of the lem/rdg is not ancient-note
            var children = app.Elements()
                .Where(e => e.Name.LocalName == "lem"
                       || e.Name.LocalName == "rdg");

            return children.Any(e => e.Attribute("type") == null
                || e.Attribute("type").Value != "ancient-note");
        }

        private static void WriteAppXml(
            AppElemLocations appWithLocs, TextWriter writer)
        {
            writer.WriteLine("```xml");
            writer.WriteLine(appWithLocs.Element.ToString());
            writer.WriteLine("```");
            writer.WriteLine();
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("REPORT OVERLAPS\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Input:  {_appFileMask}\n" +
                $"Output: {_outputPath}\n");

            int inputFileCount = 0;
            int overlapCount = 0;

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(Log.Logger);

            using (StreamWriter writer = new StreamWriter(_outputPath, false,
                Encoding.UTF8))
            {
                writer.WriteLine("# Overlaps Report");
                writer.WriteLine();

                writer.WriteLine($"Input: `{_appFileDir}{Path.DirectorySeparatorChar}{_appFileMask}`");
                writer.WriteLine();

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
                        AppElemLocationCollector.Collect(doc, widList, IsOverlappable);

                    // detect and report overlaps
                    for (int i = 0; i < appWithLocs.Count - 1; i++)
                    {
                        for (int j = i + 1; j < appWithLocs.Count; j++)
                        {
                            if (appWithLocs[i].Overlaps(appWithLocs[j]))
                            {
                                writer.WriteLine($"## Overlap {++overlapCount}");
                                writer.WriteLine();
                                writer.WriteLine(Path.GetFileName(filePath) +
                                    $" at {appWithLocs[i].LineNumber}");

                                // text
                                int n = 0;
                                foreach (var iw in appWithLocs[i].Locations)
                                {
                                    if (++n > 1) writer.Write(' ');
                                    writer.Write($"`{iw.Item1}`=`{iw.Item2}`");
                                }
                                writer.WriteLine();
                                writer.WriteLine();

                                // app
                                WriteAppXml(appWithLocs[i], writer);
                                WriteAppXml(appWithLocs[j], writer);
                                goto nextOuter;
                            }
                        }
                        nextOuter:
                        if (i % 10 == 0) Console.Write('.');
                    }
                    Console.WriteLine();
                }
                writer.Flush();
            }

            Console.WriteLine($"\nInput documents: {inputFileCount}");
            return Task.CompletedTask;
        }
    }
}
