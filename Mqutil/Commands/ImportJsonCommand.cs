using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mqutil.Commands
{
    public sealed class ImportJsonCommand : ICommand
    {
        private readonly string _txtFileMask;
        private readonly string _appFileMask;

        public ImportJsonCommand(string txtFileMask, string appFileMask)
        {
            _txtFileMask = txtFileMask
                ?? throw new ArgumentNullException(nameof(txtFileMask));
            _appFileMask = appFileMask
                ?? throw new ArgumentNullException(nameof(appFileMask));
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

            command.Description = "Import text and layers from JSON dumps";
            command.HelpOption("-?|-h|--help");

            CommandArgument txtArgument = command.Argument("[txt]",
                "The input JSON text files mask");
            CommandArgument appArgument = command.Argument("[app]",
                "The input JSON apparatus files mask");

            command.OnExecute(() =>
            {
                options.Command = new ImportJsonCommand(
                    txtArgument.Value,
                    appArgument.Value);
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("IMPORT JSON TEXT AND APPARATUS\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Text:  {_txtFileMask}\n" +
                $"Apparatus: {_appFileMask}\n");

            // TODO: logger
            int inputFileCount = 0;

            // for each input document
            string[] files = Directory.GetFiles(
                Path.GetDirectoryName(_txtFileMask),
                Path.GetFileName(_txtFileMask))
                .OrderBy(s => s)
                .ToArray();

            foreach (string filePath in files)
            {
                // load document
                string inputFileName = Path.GetFileNameWithoutExtension(
                    filePath);
                Console.WriteLine(filePath);
                inputFileCount++;
                // TODO
            }

            return Task.CompletedTask;
        }
    }
}
