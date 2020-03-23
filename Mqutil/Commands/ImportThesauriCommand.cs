using Cadmus.Core.Config;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Mq.Migration;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mqutil.Commands
{
    public sealed class ImportThesauriCommand : ICommand
    {
        private readonly string _inputFileMask;
        private readonly string _outputFilePath;

        public ImportThesauriCommand(string inputFileMask, string outputFilePath)
        {
            _inputFileMask = inputFileMask ??
                throw new ArgumentNullException(nameof(inputFileMask));
            _outputFilePath = outputFilePath ??
                throw new ArgumentNullException(nameof(outputFilePath));
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

            command.Description = "Import thesauri from MQDQ app documents " +
                "into the specified JSON file";
            command.HelpOption("-?|-h|--help");

            CommandArgument inputArgument = command.Argument("[input-mask]",
                "The input entries files mask");
            CommandArgument outputArgument = command.Argument("[output-file]",
                "The output JSON file path");

            command.OnExecute(() =>
            {
                options.Command = new ImportThesauriCommand(
                    inputArgument.Value,
                    outputArgument.Value);
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("IMPORT THESAURI\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Input:  {_inputFileMask}\n" +
                $"Output: {_outputFilePath}\n");

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(Log.Logger);
            Log.Logger.Information("IMPORT THESAURI");

            XmlThesaurusParser parser = new XmlThesaurusParser
            {
                Logger = loggerFactory.CreateLogger("parse-thes")
            };
            int inputFileCount = 0;
            int witCount = 0, authCount = 0;
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            using (StreamWriter writer = new StreamWriter(_outputFilePath,
                false, Encoding.UTF8))
            {
                writer.WriteLine("[");

                // for each input document
                string[] files = Directory.GetFiles(
                    Path.GetDirectoryName(_inputFileMask),
                    Path.GetFileName(_inputFileMask))
                    .OrderBy(s => s)
                    .ToArray();

                foreach (string filePath in files)
                {
                    // load document
                    string inputFileName = Path.GetFileNameWithoutExtension(
                        filePath);
                    Console.WriteLine(filePath);
                    inputFileCount++;
                    XDocument doc = XDocument.Load(filePath,
                        LoadOptions.PreserveWhitespace);

                    // parse items
                    string docId = inputFileName.Replace("-app", "");
                    Thesaurus[] thesauri = parser.Parse(doc, docId);
                    if (thesauri == null) continue;

                    witCount += thesauri[0]?.GetEntries().Count ?? 0;
                    authCount += thesauri[1]?.GetEntries().Count ?? 0;

                    for (int i = 0; i < thesauri.Length; i++)
                    {
                        if (thesauri[i] == null) continue;

                        SerializableThesaurus th = new SerializableThesaurus
                        {
                            Id = thesauri[i].Id
                        };
                        foreach (ThesaurusEntry entry in thesauri[i].GetEntries())
                            th.Entries.Add(entry);

                        string json = JsonSerializer.Serialize(th, options);
                        writer.Write(json);
                        writer.WriteLine(",");
                    }
                }
                writer.WriteLine("]");
                writer.Flush();
            }

            Console.WriteLine($"\nInput documents: {inputFileCount}");
            Console.WriteLine($"Witnesses: {witCount}");
            Console.WriteLine($"Authors: {authCount}");

            return Task.CompletedTask;
        }
    }

    internal sealed class SerializableThesaurus
    {
        public string Id { get; set; }

        public IList<ThesaurusEntry> Entries { get; }

        public SerializableThesaurus()
        {
            Entries = new List<ThesaurusEntry>();
        }
    }
}
