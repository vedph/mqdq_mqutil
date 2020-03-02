using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Mq.Migration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mqutil.Commands
{
    public sealed class ParseApparatusCommand : ICommand
    {
        private readonly string _inputFileMask;
        private readonly string _textDumpDir;
        private readonly string _outputDir;
        private readonly int _maxItemPerFile;

        private readonly JsonTextIndex _textIndex;

        public ParseApparatusCommand(string inputFileMask, string textDumpDir,
            string outputDir,
            int maxItemPerFile)
        {
            _inputFileMask = inputFileMask ??
                throw new ArgumentNullException(nameof(inputFileMask));
            _textDumpDir = textDumpDir ??
                throw new ArgumentNullException(nameof(textDumpDir));
            _outputDir = outputDir ??
                throw new ArgumentNullException(nameof(outputDir));
            _maxItemPerFile = maxItemPerFile;

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

            CommandArgument inputArgument = command.Argument("[input]",
                "The input entries files mask");
            CommandArgument textDumpArgument = command.Argument("[text dump]",
                "The input text dumps directory");
            CommandArgument outputArgument = command.Argument("[output]",
                "The output directory");

            CommandOption maxItemPerFileOption = command.Option("-m|--max",
                "Max number of items per output file",
                CommandOptionType.SingleValue);

            command.OnExecute(() =>
            {
                int max = 100;
                if (maxItemPerFileOption.HasValue()
                    && int.TryParse(maxItemPerFileOption.Value(), out int n))
                {
                    max = n;
                }
                options.Command = new ParseApparatusCommand(
                    inputArgument.Value,
                    textDumpArgument.Value,
                    outputArgument.Value,
                    max);
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
            XmlApparatusParser parser = new XmlApparatusParser
            {
                Logger = loggerFactory.CreateLogger("parse-app")
            };

            int inputFileCount = 0;
            int totalItemCount = 0;
            StreamWriter writer = null;
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            if (!Directory.Exists(_outputDir))
                Directory.CreateDirectory(_outputDir);

            // for each input document
            foreach (string filePath in Directory.GetFiles(
                Path.GetDirectoryName(_inputFileMask),
                Path.GetFileName(_inputFileMask))
                .OrderBy(s => s))
            {
                // load document
                string inputFileName = Path.GetFileNameWithoutExtension(filePath);
                Console.WriteLine(filePath);
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

                // load index
                LoadTextIndex(inputFileName.Replace("-app", ""));

                // parse
                int partCount = 0, outputFileCount = 0;

                foreach (var part in parser.Parse(
                    doc, Path.GetFileNameWithoutExtension(filePath), _textIndex))
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
                    string json = JsonConvert.SerializeObject(
                        part, jsonSettings);
                    writer.WriteLine(json + ",");
                }
                totalItemCount += partCount;
                if (writer != null) CloseOutputFile(writer);
            }

            Console.WriteLine($"\nInput documents: {inputFileCount}");
            Console.WriteLine($"Output parts: {totalItemCount}");

            return Task.CompletedTask;
        }
    }
}
