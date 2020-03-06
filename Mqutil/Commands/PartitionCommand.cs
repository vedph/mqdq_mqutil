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
        private readonly string _inputFileMask;
        private readonly string _outputDir;
        private readonly int _minTreshold;
        private readonly int _maxTreshold;
        private readonly bool _regexMask;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartitionCommand"/> class.
        /// </summary>
        /// <param name="inputFileMask">The input file(s) mask.</param>
        /// <param name="outputDir">The output directory.</param>
        /// <param name="minTreshold">The minimum l treshold.</param>
        /// <param name="maxTreshold">The maximum l treshold.</param>
        /// <param name="regexMask">True if file mask is a regular expression.
        /// </param>
        /// <exception cref="ArgumentNullException">inputFileMask or
        /// outputDir</exception>
        public PartitionCommand(string inputFileMask, string outputDir,
            int minTreshold, int maxTreshold, bool regexMask)
        {
            _inputFileMask = inputFileMask ??
                throw new ArgumentNullException(nameof(inputFileMask));
            _outputDir = outputDir ??
                throw new ArgumentNullException(nameof(outputDir));
            _minTreshold = minTreshold;
            _maxTreshold = maxTreshold;
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

            command.Description = "Partition the MQDQ text documents " +
                "saving the results into the specified folder";
            command.HelpOption("-?|-h|--help");

            CommandArgument inputArgument = command.Argument("[input]",
                "The input entries files mask");
            CommandArgument outputArgument = command.Argument("[output]",
                "The output directory");

            CommandOption minTresholdOption = command.Option("-n|--min",
                "The minimum l-count treshold", CommandOptionType.SingleValue);
            CommandOption maxTresholdOption = command.Option("-m|--max",
                "The maximum l-count treshold", CommandOptionType.SingleValue);
            CommandOption regexMaskOption = command.Option("-r|--regex",
                "Use regular expressions in files masks", CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                options.Command = new PartitionCommand(
                    inputArgument.Value,
                    outputArgument.Value,
                    minTresholdOption.HasValue()
                    ? int.Parse(minTresholdOption.Value(), CultureInfo.InvariantCulture)
                    : 20,
                    maxTresholdOption.HasValue()
                    ? int.Parse(minTresholdOption.Value(), CultureInfo.InvariantCulture)
                    : 50,
                    regexMaskOption.HasValue());
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
                $"Input:  {_inputFileMask}\n" +
                $"Output: {_outputDir}\n" +
                $"Min: {_minTreshold}\n" +
                $"Max: {_maxTreshold}\n");

            XmlPartitioner partitioner = new XmlPartitioner
            {
                MinTreshold = _minTreshold,
                MaxTreshold = _maxTreshold
            };

            int count = 0;
            foreach (string filePath in FileEnumerator.Enumerate(
                Path.GetDirectoryName(_inputFileMask),
                Path.GetFileName(_inputFileMask),
                _regexMask))
            {
                Console.Write(filePath);

                XDocument doc = XDocument.Load(filePath,
                    LoadOptions.PreserveWhitespace);

                bool touched = partitioner.Partition(doc,
                    Path.GetFileNameWithoutExtension(filePath));

                if (touched)
                {
                    count++;
                    string outputPath =
                        Path.Combine(_outputDir, Path.GetFileName(filePath));
                    Console.WriteLine($" => {outputPath}");
                    if (!Directory.Exists(_outputDir))
                        Directory.CreateDirectory(_outputDir);
                    doc.Save(outputPath, SaveOptions.OmitDuplicateNamespaces);
                }
                else
                {
                    Console.WriteLine();
                }
            }

            Console.WriteLine($"Files partitioned: {count}");

            return Task.CompletedTask;
        }
    }
}
