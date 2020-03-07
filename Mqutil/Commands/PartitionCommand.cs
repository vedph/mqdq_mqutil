using Microsoft.Extensions.CommandLineUtils;
using Mq.Migration;
using Mqutil.Services;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mqutil.Commands
{
    /// <summary>
    /// Partition MQDQ text documents where required.
    /// </summary>
    /// <seealso cref="ICommand" />
    public sealed class PartitionCommand : ICommand
    {
        private readonly string _inputDir;
        private readonly string _fileMask;
        private readonly string _outputDir;
        private readonly int _minTreshold;
        private readonly int _maxTreshold;
        private readonly bool _regexMask;
        private readonly bool _recursive;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartitionCommand"/> class.
        /// </summary>
        /// <param name="inputDir">The input file(s) directory.</param>
        /// <param name="fileMask">The input file(s) mask.</param>
        /// <param name="outputDir">The output directory.</param>
        /// <param name="minTreshold">The minimum l treshold.</param>
        /// <param name="maxTreshold">The maximum l treshold.</param>
        /// <param name="regexMask">True if file mask is a regular expression.
        /// </param>
        /// <param name="recursive">True to recurse subdirectories when
        /// matching input files.</param>
        /// <exception cref="ArgumentNullException">inputFileMask or
        /// outputDir</exception>
        public PartitionCommand(string inputDir, string fileMask, string outputDir,
            int minTreshold, int maxTreshold, bool regexMask, bool recursive)
        {
            _inputDir = inputDir ??
                throw new ArgumentNullException(nameof(inputDir));
            _fileMask = fileMask ??
                throw new ArgumentNullException(nameof(fileMask));
            _outputDir = outputDir ??
                throw new ArgumentNullException(nameof(outputDir));
            _minTreshold = minTreshold;
            _maxTreshold = maxTreshold;
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

            command.Description = "Partition the MQDQ text documents " +
                "saving the results into the specified folder";
            command.HelpOption("-?|-h|--help");

            CommandArgument inputArgument = command.Argument("[input-dir]",
                "The input files directory");
            CommandArgument fileMaskArgument = command.Argument("[file-mask]",
                "The input files mask");
            CommandArgument outputArgument = command.Argument("[output-dir]",
                "The output directory");

            CommandOption minTresholdOption = command.Option("-n|--min",
                "The minimum l-count treshold", CommandOptionType.SingleValue);
            CommandOption maxTresholdOption = command.Option("-m|--max",
                "The maximum l-count treshold", CommandOptionType.SingleValue);
            CommandOption regexMaskOption = command.Option("-r|--regex",
                "Use regular expressions in files masks", CommandOptionType.NoValue);
            CommandOption recursiveOption = command.Option("-s|--sub",
                "Recurse subdirectories in matching files masks",
                CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                options.Command = new PartitionCommand(
                    inputArgument.Value,
                    fileMaskArgument.Value,
                    outputArgument.Value,
                    minTresholdOption.HasValue()
                    ? int.Parse(minTresholdOption.Value(), CultureInfo.InvariantCulture)
                    : 20,
                    maxTresholdOption.HasValue()
                    ? int.Parse(minTresholdOption.Value(), CultureInfo.InvariantCulture)
                    : 50,
                    regexMaskOption.HasValue(),
                    recursiveOption.HasValue());
                return 0;
            });
        }

        /// <summary>
        /// Runs this command.
        /// </summary>
        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PARTITION\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Input dir:  {_inputDir}\n" +
                $"Input mask: {_fileMask}\n" +
                $"Output dir: {_outputDir}\n" +
                $"Min: {_minTreshold}\n" +
                $"Max: {_maxTreshold}\n" +
                $"Recursive: {_recursive}\n");

            XmlPartitioner partitioner = new XmlPartitioner
            {
                MinTreshold = _minTreshold,
                MaxTreshold = _maxTreshold
            };

            int partitioned = 0, total = 0;

            if (!Directory.Exists(_outputDir))
                Directory.CreateDirectory(_outputDir);

            foreach (string filePath in FileEnumerator.Enumerate(
                _inputDir, _fileMask, _regexMask, _recursive))
            {
                total++;
                Console.Write(filePath);

                XDocument doc = XDocument.Load(filePath,
                    LoadOptions.PreserveWhitespace);

                bool touched = partitioner.Partition(doc,
                    Path.GetFileNameWithoutExtension(filePath));

                string outputPath =
                    Path.Combine(_outputDir, Path.GetFileName(filePath));

                if (touched)
                {
                    partitioned++;
                    Console.WriteLine($" => {outputPath}");
                    if (!Directory.Exists(_outputDir))
                        Directory.CreateDirectory(_outputDir);
                    doc.Save(outputPath, SaveOptions.OmitDuplicateNamespaces);
                }
                else
                {
                    File.Copy(filePath, outputPath);
                    Console.WriteLine();
                }
            }

            Console.WriteLine($"Total files: {total}");
            Console.WriteLine($"Partitioned files: {partitioned}");

            return Task.CompletedTask;
        }
    }
}
