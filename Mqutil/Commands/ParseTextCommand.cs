using Cadmus.Core;
using Microsoft.Extensions.CommandLineUtils;
using Mq.Migration;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mqutil.Commands
{
    public sealed class ParseTextCommand : ICommand
    {
        private readonly string _inputFileMask;
        private readonly string _outputDir;
        private readonly int _maxItemPerFile;

        public ParseTextCommand(string inputFileMask, string outputDir,
            int maxItemPerFile)
        {
            _inputFileMask = inputFileMask ??
                throw new ArgumentNullException(nameof(inputFileMask));
            _outputDir = outputDir ??
                throw new ArgumentNullException(nameof(outputDir));
            _maxItemPerFile = maxItemPerFile;
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

            CommandArgument inputArgument = command.Argument("[input]",
                "The input entries files mask");
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
                options.Command = new ParseTextCommand(
                    inputArgument.Value,
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

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PARSE TEXT\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Input:  {_inputFileMask}\n" +
                $"Output: {_outputDir}\n" +
                $"Max items per file: {_maxItemPerFile}\n");

            XmlTextParser parser = new XmlTextParser();
            // TODO: logger
            int inputFileCount = 0;
            int totalItemCount = 0;
            StreamWriter writer = null;
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            // for each input document
            foreach (string filePath in Directory.GetFiles(
                Path.GetDirectoryName(_inputFileMask),
                Path.GetFileName(_inputFileMask))
                .OrderBy(s => s))
            {
                string inputFileName = Path.GetFileNameWithoutExtension(filePath);
                inputFileCount++;
                int itemCount = 0, outputFileCount = 0;
                Console.Write(filePath);
                XDocument doc = XDocument.Load(filePath,
                    LoadOptions.PreserveWhitespace);

                // parse items
                foreach (IItem item in parser.Parse(
                    doc, Path.GetFileName(filePath)))
                {
                    itemCount++;

                    // create new output file if required
                    if (writer == null
                        || (_maxItemPerFile > 0 && itemCount > _maxItemPerFile))
                    {
                        if (writer != null) CloseOutputFile(writer);
                        else if (!Directory.Exists(_outputDir))
                            Directory.CreateDirectory(_outputDir);
                        string path = Path.Combine(_outputDir,
                            $"{inputFileName}_{++outputFileCount:00000}.json");

                        writer = new StreamWriter(new FileStream(path,
                            FileMode.Create, FileAccess.Write, FileShare.Read),
                            Encoding.UTF8);
                        writer.WriteLine("[");
                    }

                    // dump item into it
                    string json = JsonSerializer.Serialize(item, options);
                    writer.WriteLine(json);
                }
                totalItemCount += itemCount;
                if (writer != null) CloseOutputFile(writer);
            }
            if (writer != null) CloseOutputFile(writer);

            Console.WriteLine($"Input documents: {inputFileCount}");
            Console.WriteLine($"Output items: {totalItemCount}");

            return Task.CompletedTask;
        }
    }
}
