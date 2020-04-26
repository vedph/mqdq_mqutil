using Microsoft.Extensions.CommandLineUtils;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mqutil.Commands
{
    public sealed class PrepareExportCommand : ICommand
    {
        private readonly string _txtDir;
        private readonly string _appDir;
        private readonly string _outDir;

        public PrepareExportCommand(string txtDir, string appDir, string outDir)
        {
            _txtDir = txtDir ?? throw new ArgumentNullException(nameof(txtDir));
            _appDir = appDir ?? throw new ArgumentNullException(nameof(appDir));
            _outDir = outDir ?? throw new ArgumentNullException(nameof(outDir));
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

            command.Description = "Prepare the export directory by copying " +
                "original text files and processed apparatus files";
            command.HelpOption("-?|-h|--help");

            CommandArgument txtDirArgument = command.Argument("[text-dir]",
                "The original text files directory");
            CommandArgument appDirArgument = command.Argument("[apparatus-dir]",
                "The processed apparatus files directory");
            CommandArgument outDirArgument = command.Argument("[output-dir]",
                "The output directory (will be cleared if existing)");

            command.OnExecute(() =>
            {
                options.Command = new PrepareExportCommand(
                    txtDirArgument.Value,
                    appDirArgument.Value,
                    outDirArgument.Value);
                return 0;
            });
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("PREPARE EXPORT TEI FILES\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Text dir: {_txtDir}\n" +
                $"Apparatus dir: {_appDir}\n" +
                $"Output dir: {_outDir}\n");

            Regex appFileNameRegex = new Regex(@"-app\.xml$", RegexOptions.IgnoreCase);

            // clear output dir
            if (Directory.Exists(_outDir)) Directory.Delete(_outDir, true);
            Directory.CreateDirectory(_outDir);

            foreach (string subDir in Directory.EnumerateDirectories(_txtDir))
            {
                foreach (string txtFile in
                    Directory.EnumerateFiles(subDir, "*.xml"))
                {
                    // ignore app files
                    if (appFileNameRegex.IsMatch(txtFile)) continue;

                    Console.WriteLine(txtFile);

                    // create subdir in output
                    string outSubDir = Path.Combine(_outDir, Path.GetFileName(subDir));
                    Directory.CreateDirectory(outSubDir);

                    // copy text file in output subdir
                    File.Copy(
                        txtFile,
                        Path.Combine(outSubDir, Path.GetFileName(txtFile)));

                    // copy app file from appDir
                    string appFile = Path.Combine(
                        _appDir,
                        Path.GetFileNameWithoutExtension(txtFile) + "-app.xml");

                    if (File.Exists(appFile))
                    {
                        File.Copy(
                            appFile,
                            Path.Combine(outSubDir, Path.GetFileName(appFile)));
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
