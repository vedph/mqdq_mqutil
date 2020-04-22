using Cadmus.Core;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Mq.Migration;
using Mqutil.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mqutil.Commands
{
    public sealed class ParseTextCommand : ICommand
    {
        private readonly string _inputFileDir;
        private readonly string _inputFileMask;
        private readonly string _outputDir;
        private readonly string _flagDivIdList;
        private readonly int _maxItemPerFile;
        private readonly bool _regexMask;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParseTextCommand" /> class.
        /// </summary>
        /// <param name="inputFileDir">The input files directory.</param>
        /// <param name="inputFileMask">The input files mask.</param>
        /// <param name="outputDir">The output dir.</param>
        /// <param name="flagDivIdList">The optional div identifiers list to be
        /// used for flagging with 1 items matching them.</param>
        /// <param name="maxItemPerFile">The maximum item per file.</param>
        /// <param name="regexMask">True if file mask is a regular expression.</param>
        /// <exception cref="ArgumentNullException">inputFileMask or outputDir</exception>
        public ParseTextCommand(string inputFileDir, string inputFileMask,
            string outputDir, string flagDivIdList, int maxItemPerFile, bool regexMask)
        {
            _inputFileDir = inputFileDir ??
                throw new ArgumentNullException(nameof(inputFileDir));
            _inputFileMask = inputFileMask ??
                throw new ArgumentNullException(nameof(inputFileMask));
            _outputDir = outputDir ??
                throw new ArgumentNullException(nameof(outputDir));
            _flagDivIdList = flagDivIdList;
            _maxItemPerFile = maxItemPerFile;
            _regexMask = regexMask;
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

            command.Description = "Parse the MQDQ text documents " +
                "dumping the results into the specified folder";
            command.HelpOption("-?|-h|--help");

            CommandArgument inputDirArgument = command.Argument("[input-dir]",
                "The input entries files directory");
            CommandArgument inputMaskArgument = command.Argument("[input-mask]",
                "The input entries files mask");
            CommandArgument outputArgument = command.Argument("[output]",
                "The output directory");

            CommandOption flagDivIdListOption = command.Option("-d|--div-list",
                "The path to the div IDs list file used to flag items with 1",
                CommandOptionType.SingleValue);
            CommandOption maxItemPerFileOption = command.Option("-m|--max",
                "Max number of items per output file",
                CommandOptionType.SingleValue);
            CommandOption regexMaskOption = command.Option("-r|--regex",
                "Use regular expressions in files masks", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                int max = 100;
                if (maxItemPerFileOption.HasValue()
                    && int.TryParse(maxItemPerFileOption.Value(), out int n))
                {
                    max = n;
                }
                options.Command = new ParseTextCommand(
                    inputDirArgument.Value,
                    inputMaskArgument.Value,
                    outputArgument.Value,
                    flagDivIdListOption.Value(),
                    max,
                    regexMaskOption.HasValue());
                return 0;
            });
        }

        private static void CloseOutputFile(TextWriter writer)
        {
            writer.WriteLine("]");
            writer.Flush();
            writer.Close();
        }

        private static Dictionary<string, HashSet<string>> LoadDivIds(
            string path, string prefix)
        {
            Dictionary<string, HashSet<string>> ids =
                new Dictionary<string, HashSet<string>>();

            using (StreamReader reader = new StreamReader(path, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        int i = line.IndexOf(' ');
                        Debug.Assert(i > -1);
                        string docId = line.Substring(0, i);
                        string divId = line.Substring(i + 1);

                        if (!ids.ContainsKey(docId))
                            ids[docId] = new HashSet<string>();
                        ids[docId].Add(prefix + divId);
                    }
                }
            }
            return ids;
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PARSE TEXT\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Input dir:  {_inputFileDir}\n" +
                $"Input mask: {_inputFileMask}\n" +
                $"Output dir: {_outputDir}\n" +
                $"Div IDs list: {_flagDivIdList ?? "(none)"}\n" +
                $"Max items per file: {_maxItemPerFile}\n");

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(Log.Logger);
            Log.Logger.Information("PARSE TEXT");

            XmlTextParser parser = new XmlTextParser
            {
                Logger = loggerFactory.CreateLogger("parse-text")
            };

            int inputFileCount = 0;
            int totalItemCount = 0;
            StreamWriter writer = null;

            if (!Directory.Exists(_outputDir))
                Directory.CreateDirectory(_outputDir);

            // load div IDs list if requested, prefixing and suffixing them
            // so that we are ready to find them in the item's title
            Dictionary<string, HashSet<string>> flagDivIds =
                _flagDivIdList != null
                ? LoadDivIds(_flagDivIdList, "#")
                : null;

            // for each input document
            foreach (string filePath in FileEnumerator.Enumerate(
                _inputFileDir, _inputFileMask, _regexMask))
            {
                // load document
                string inputFileName = Path.GetFileNameWithoutExtension(filePath);
                Console.WriteLine("\n" + filePath);
                inputFileCount++;
                XDocument doc = XDocument.Load(filePath,
                    LoadOptions.PreserveWhitespace);
                JsonSerializerSettings jsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    },
                    Formatting = Formatting.Indented
                };

                // parse items
                int itemCount = 0, outputFileCount = 0;

                foreach (IItem item in parser.Parse(
                    doc, Path.GetFileNameWithoutExtension(filePath)))
                {
                    if (++itemCount % 10 == 0) Console.Write('.');

                    // set flag if required
                    if (flagDivIds.ContainsKey(inputFileName)
                        && flagDivIds[inputFileName].Any(s =>
                        item.Title.EndsWith(s, StringComparison.Ordinal)))
                    {
                        item.Flags |= 1;
                        Log.Logger.Information($"Flagged item {item}");
                    }

                    // create new output file if required
                    if (writer == null
                        || (_maxItemPerFile > 0 && itemCount > _maxItemPerFile))
                    {
                        if (writer != null) CloseOutputFile(writer);
                        string path = Path.Combine(_outputDir,
                            $"{inputFileName}_{++outputFileCount:00000}.json");

                        writer = new StreamWriter(new FileStream(path,
                            FileMode.Create, FileAccess.Write, FileShare.Read),
                            Encoding.UTF8);
                        writer.WriteLine("[");
                    }

                    // dump item into it
                    string json = JsonConvert.SerializeObject(
                        item, jsonSettings);
                    // string json = JsonSerializer.Serialize(item, typeof(object), options);
                    // this will output a , also for the last JSON array item,
                    // but we don't care about it -- that's just a dump, and
                    // it's easy to ignore/remove it if needed.
                    writer.WriteLine(json + ",");
                }
                totalItemCount += itemCount;
                if (writer != null)
                {
                    CloseOutputFile(writer);
                    writer = null;
                }
            }

            Console.WriteLine($"\nInput documents: {inputFileCount}");
            Console.WriteLine($"Output items: {totalItemCount}");

            return Task.CompletedTask;
        }
    }
}
