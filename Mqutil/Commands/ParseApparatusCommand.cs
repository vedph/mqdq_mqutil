using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Mq.Migration;
using Mqutil.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mqutil.Commands
{
    public sealed class ParseApparatusCommand : ICommand
    {
        private readonly string _inputFileDir;
        private readonly string _inputFileMask;
        private readonly string _textDumpDir;
        private readonly string _outputDir;
        private readonly int _maxItemPerFile;
        private readonly bool _regexMask;
        private readonly bool _recursive;

        private readonly JsonTextIndex _textIndex;

        public ParseApparatusCommand(string inputFileDir,
            string inputFileMask, string textDumpDir,
            string outputDir, int maxItemPerFile, bool regexMask,
            bool recursive)
        {
            _inputFileDir = inputFileDir ??
                throw new ArgumentNullException(nameof(inputFileDir));
            _inputFileMask = inputFileMask ??
                throw new ArgumentNullException(nameof(inputFileMask));
            _textDumpDir = textDumpDir ??
                throw new ArgumentNullException(nameof(textDumpDir));
            _outputDir = outputDir ??
                throw new ArgumentNullException(nameof(outputDir));
            _maxItemPerFile = maxItemPerFile;
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
                "dumping the results into the specified folder";
            command.HelpOption("-?|-h|--help");

            CommandArgument inputDirArgument = command.Argument("[input-dir]",
                "The input entries files directory");
            CommandArgument inputMaskArgument = command.Argument("[input-mask]",
                "The input entries files mask");
            CommandArgument txtDumpDirArgument = command.Argument("[text-dump-dir]",
                "The input text dumps directory");
            CommandArgument outputDirArgument = command.Argument("[output-dir]",
                "The output directory");

            CommandOption maxItemPerFileOption = command.Option("-m|--max",
                "Max number of items per output file",
                CommandOptionType.SingleValue);
            CommandOption regexMaskOption = command.Option("-r|--regex",
                "Use regular expressions in files masks", CommandOptionType.NoValue);
            CommandOption recursiveOption = command.Option("-s|--sub",
                "Recurse subdirectories in matching files masks",
                CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                int max = 100;
                if (maxItemPerFileOption.HasValue()
                    && int.TryParse(maxItemPerFileOption.Value(), out int n))
                {
                    max = n;
                }
                options.Command = new ParseApparatusCommand(
                    inputDirArgument.Value,
                    inputMaskArgument.Value,
                    txtDumpDirArgument.Value,
                    outputDirArgument.Value,
                    max,
                    regexMaskOption.HasValue(),
                    recursiveOption.HasValue());
                return 0;
            });
        }

        private static void CloseOutputFile(TextWriter writer)
        {
            writer.WriteLine("]");
            writer.Flush();
            writer.Close();
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
                    _textIndex.Index(stream);
                }
            }
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PARSE APPARATUS\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Input:  {_inputFileMask}\n" +
                $"Output: {_outputDir}\n" +
                $"Max items per file: {_maxItemPerFile}\n");

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(Log.Logger);
            Log.Logger.Information("PARSE APPARATUS");

            XmlApparatusParser parser = new XmlApparatusParser
            {
                Logger = loggerFactory.CreateLogger("parse-app")
            };

            int inputFileCount = 0;
            int totalPartCount = 0;
            StreamWriter writer = null;

            if (!Directory.Exists(_outputDir))
                Directory.CreateDirectory(_outputDir);

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
                JsonSerializerSettings jsonSettings = new JsonSerializerSettings
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new CamelCaseNamingStrategy()
                    },
                    Formatting = Formatting.Indented
                };

                // load index
                LoadTextIndex(inputFileName.Replace("-app", ""));

                // parse
                int partCount = 0, outputFileCount = 0;

                foreach (var part in parser.Parse(doc, inputFileName, _textIndex))
                {
                    if (++partCount % 10 == 0) Console.Write('.');

                    // create new output file if required
                    if (writer == null
                        || (_maxItemPerFile > 0 && partCount > _maxItemPerFile))
                    {
                        if (writer != null) CloseOutputFile(writer);
                        string path = Path.Combine(_outputDir,
                            $"{inputFileName}_{++outputFileCount:00000}.json");

                        writer = new StreamWriter(new FileStream(path,
                            FileMode.Create, FileAccess.Write, FileShare.Read),
                            Encoding.UTF8);
                        writer.WriteLine("[");
                    }

                    // dump part into it
                    string json = JsonConvert.SerializeObject(part, jsonSettings);
                    writer.WriteLine(json + ",");
                }
                totalPartCount += partCount;
                if (writer != null)
                {
                    CloseOutputFile(writer);
                    writer = null;
                }
            }

            Console.WriteLine($"\nInput documents: {inputFileCount}");
            Console.WriteLine($"Output parts: {totalPartCount}");

            return Task.CompletedTask;
        }
    }
}
