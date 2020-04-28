using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Mq.Migration;
using Mqutil.Services;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mqutil.Commands
{
    public sealed class AddCreditCommand : ICommand
    {
        private readonly string _txtDir;
        private readonly string _respValue;
        private readonly string _persValue;
        private readonly bool _dry;

        public AddCreditCommand(string txtDir, string respValue, string persValue,
            bool dry)
        {
            _txtDir = txtDir ?? throw new ArgumentNullException(nameof(txtDir));
            _respValue = respValue ?? throw new ArgumentNullException(nameof(respValue));
            _persValue = persValue ?? throw new ArgumentNullException(nameof(persValue));
            _dry = dry;
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

            command.Description = "Add credit to text files in their subdirectories";
            command.HelpOption("-?|-h|--help");

            CommandArgument txtDirArgument = command.Argument("[txt-dir]",
                "The TEI text files root directory");
            CommandArgument respValueArgument = command.Argument("[resp-value]",
                "The resp element value");
            CommandArgument persValueArgument = command.Argument("[pers-value]",
                "The persName element value");

            CommandOption dryOption = command.Option("-d|--dry", "Dry run",
                CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                options.Command = new AddCreditCommand(
                    txtDirArgument.Value,
                    respValueArgument.Value,
                    persValueArgument.Value,
                    dryOption.HasValue());
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ADD CREDIT TO TEXT FILES\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Text dir:  {_txtDir}\n" +
                $"Resp value: {_respValue}\n" +
                $"PersName value: {_persValue}\n" +
                $"Dry run: {_dry}\n");

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(Log.Logger);
            Log.Logger.Information("ADD CREDIT TO TEXT FILES");

            foreach (string filePath in FileEnumerator.Enumerate(_txtDir,
                @"^[^-]+-[^-]+\.xml", true, true))
            {
                Console.WriteLine(filePath);
                Log.Logger.Information(filePath);

                XDocument doc = XDocument.Load(filePath,
                    LoadOptions.PreserveWhitespace);

                // TEI/teiHeader/fileDesc/seriesStmt/
                XElement series = doc.Root
                    ?.Element(XmlHelper.TEI + "teiHeader")
                    ?.Element(XmlHelper.TEI + "fileDesc")
                    ?.Element(XmlHelper.TEI + "seriesStmt");
                if (series == null)
                {
                    Log.Logger?.Error(
                        $"Unable to find seriesStmt in header for {Path.GetFileName(filePath)}");
                    continue;
                }

                // <respStmt>
                //   <resp key="MQDQ">RESPVALUE</resp>
                //   <persName>PERSVALUE</persName>
                // </respStmt>
                series.Add(new XElement(XmlHelper.TEI + "respStmt",
                    new XElement(XmlHelper.TEI + "resp",
                        new XAttribute("key", "MQDQ"),
                        _respValue),
                    new XElement(XmlHelper.TEI + "persName", _persValue)));

                if (!_dry) doc.Save(filePath);
            }

            return Task.CompletedTask;
        }
    }
}
