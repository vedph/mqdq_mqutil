using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Threading.Tasks;

namespace Mqutil.Commands
{
    public sealed class RootCommand : ICommand
    {
        private readonly CommandLineApplication _app;

        public RootCommand(CommandLineApplication app)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
        }

        public static void Configure(CommandLineApplication app, AppOptions options)
        {
            // configure all the app commands here
            app.Command("partition", c => PartitionCommand.Configure(c, options));
            app.Command("parse-text", c => ParseTextCommand.Configure(c, options));
            app.Command("parse-app", c => ParseApparatusCommand.Configure(c, options));
            app.Command("report-overlaps", c => ReportOverlapsCommand.Configure(c, options));
            app.Command("remove-overlaps", c => RemoveOverlapsCommand.Configure(c, options));
            app.Command("import-thes", c => ImportThesauriCommand.Configure(c, options));
            app.Command("import-json", c => ImportJsonCommand.Configure(c, options));
            app.Command("prepare-export", c => PrepareExportCommand.Configure(c, options));
            app.Command("export-text", c => ExportTextCommand.Configure(c, options));
            app.Command("export-app", c => ExportApparatusCommand.Configure(c, options));
            app.Command("add-credit", c => AddCreditCommand.Configure(c, options));

            app.OnExecute(() =>
            {
                options.Command = new RootCommand(app);
                return 0;
            });
        }

        public Task Run()
        {
            _app.ShowHelp();
            return Task.FromResult(0);
        }
    }
}
