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

namespace Mqutil.Commands
{
    public sealed class ReportOverlapsCommand : ICommand
    {
        private readonly string _inputFileDir;
        private readonly string _inputFileMask;
        private readonly string _textDumpDir;
        private readonly string _outputPath;
        private readonly bool _regexMask;
        private readonly bool _recursive;
        private readonly JsonTextIndex _textIndex;

        public ReportOverlapsCommand(string inputFileDir,
            string inputFileMask, string textDumpDir,
            string outputPath, bool regexMask,
            bool recursive)
        {
            _inputFileDir = inputFileDir ??
                throw new ArgumentNullException(nameof(inputFileDir));
            _inputFileMask = inputFileMask ??
                throw new ArgumentNullException(nameof(inputFileMask));
            _textDumpDir = textDumpDir ??
                throw new ArgumentNullException(nameof(textDumpDir));
            _outputPath = outputPath ??
                throw new ArgumentNullException(nameof(outputPath));
            _regexMask = regexMask;
            _recursive = recursive;

            _textIndex = new JsonTextIndex();
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

            command.Description = "Parse the MQDQ apparatus documents " +
                "creating an overlaps report into the specified file";
            command.HelpOption("-?|-h|--help");

            CommandArgument inputDirArgument = command.Argument("[input-dir]",
                "The input entries files directory");
            CommandArgument inputMaskArgument = command.Argument("[input-mask]",
                "The input entries files mask");
            CommandArgument txtDumpDirArgument = command.Argument("[text-dump-dir]",
                "The input text dumps directory");
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
                    inputDirArgument.Value,
                    inputMaskArgument.Value,
                    txtDumpDirArgument.Value,
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

        private void LoadTextIndex(string inputFileName)
        {
            _textIndex.Clear();
            foreach (string jsonFile in
                Directory.EnumerateFiles(_textDumpDir, inputFileName + "_*.json"))
            {
                using (Stream stream = new FileStream(jsonFile,
                    FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    _textIndex.Index(stream, true);
                }
            }
        }

        private List<Tuple<XElement, MqIdAppSet>> CollectAppWithLocations(
            XDocument doc)
        {
            // collect app locations
            char[] wsSeps = new[] { ' ' };
            List<Tuple<XElement, MqIdAppSet>> appWithSets =
                new List<Tuple<XElement, MqIdAppSet>>();

            foreach (XElement appElem in XmlHelper.GetTeiBody(doc)
                .Descendants(XmlHelper.TEI + "app")
                .Where(IsOverlappable))
            {
                MqIdAppSet set = new MqIdAppSet();

                if (appElem.Attribute("loc") != null)
                {
                    set.SetLoc(from id in appElem.Attribute("loc").Value
                        .Split(wsSeps, StringSplitOptions.RemoveEmptyEntries)
                               select MqId.Parse(id));
                }
                else
                {
                    MqId fromId = MqId.Parse(appElem.Attribute("from").Value);
                    MqId toId = MqId.Parse(appElem.Attribute("to").Value);
                    if (fromId == null)
                    {
                        // skip, these are fragments having a different ID
                        // scheme, and it's quickier to ignore them as they
                        // are short texts
                        continue;
                    }

                    set.SetFromTo(fromId, toId);
                }
                appWithSets.Add(Tuple.Create(appElem, set));
            }
            return appWithSets;
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("REPORT OVERLAPS\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Input:  {_inputFileMask}\n" +
                $"Output: {_outputPath}\n");

            int inputFileCount = 0;
            int overlapCount = 0;

            using (StreamWriter writer = new StreamWriter(_outputPath, false,
                Encoding.UTF8))
            {
                writer.WriteLine("# Overlaps Report");
                writer.WriteLine($"{_inputFileMask}{Path.DirectorySeparatorChar}{_inputFileMask}");

                // for each input document
                foreach (string filePath in FileEnumerator.Enumerate(
                    _inputFileDir, _inputFileMask, _regexMask, _recursive))
                {
                    Console.WriteLine();
                    Log.Logger.Information("Parsing {FilePath}", filePath);

                    // load document
                    string inputFileName = Path.GetFileNameWithoutExtension(filePath);
                    Console.WriteLine(filePath);
                    inputFileCount++;
                    XDocument doc = XDocument.Load(filePath,
                        LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

                    // load index
                    LoadTextIndex(inputFileName.Replace("-app", ""));

                    // collect app's locations
                    List<Tuple<XElement, MqIdAppSet>> appWithLocs =
                        CollectAppWithLocations(doc);

                    // detect and report overlaps
                    for (int i = 0; i < appWithLocs.Count; i++)
                    {
                        for (int j = i + 1; j < appWithLocs.Count; j++)
                        {
                            if (appWithLocs[i].Item2.Overlaps(appWithLocs[j].Item2))
                            {
                                writer.WriteLine($"## Overlap {++overlapCount}");
                                writer.WriteLine(Path.GetFileName(filePath));
                                // text
                                MqIdAppSet set = appWithLocs[i].Item2;
                                int idNr = 0;
                                foreach (MqId id in set.GetIds())
                                {
                                    if (++idNr > 1) writer.Write(' ');
                                    string s = id.ToString();
                                    writer.Write($"`{s}`=`{_textIndex.Find(s)}`");
                                }
                                writer.WriteLine();
                                // app
                                writer.WriteLine("```xml");
                                writer.WriteLine(appWithLocs[i].Item1.ToString());
                                writer.WriteLine("```");
                                writer.WriteLine();
                            }
                        }
                    }
                }
                writer.Flush();
            }

            Console.WriteLine($"\nInput documents: {inputFileCount}");
            return Task.CompletedTask;
        }
    }
}
