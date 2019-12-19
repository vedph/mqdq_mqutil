using Mqutil.Commands;
using Microsoft.Extensions.CommandLineUtils;
using System;

namespace Mqutil
{
    public sealed class AppOptions
    {
        public ICommand Command { get; set; }

        public static AppOptions Parse(string[] args)
        {
            if (args == null) throw new ArgumentNullException(nameof(args));

            AppOptions options = new AppOptions();
            CommandLineApplication app = new CommandLineApplication
            {
                // TODO: customize the app template names
                Name = "Mqutil",
                FullName = "App CLI"
            };
            app.HelpOption("-?|-h|--help");

            // app-level options
            RootCommand.Configure(app, options);

            int result = app.Execute(args);
            return result != 0 ? null : options;
        }
    }
}
