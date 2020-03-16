using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Mq.Migration;
using Mqutil.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Mqutil.Commands
{
    public sealed class RemoveOverlapsCommand : ICommand
    {
        private readonly string _appFileDir;
        private readonly string _appFileMask;
        private readonly string _outputDir;
        private readonly bool _regexMask;
        private readonly bool _recursive;

        public RemoveOverlapsCommand(string appFileDir,
            string appFileMask, string outputDir, bool regexMask,
            bool recursive)
        {
            _appFileDir = appFileDir ??
                throw new ArgumentNullException(nameof(appFileDir));
            _appFileMask = appFileMask ??
                throw new ArgumentNullException(nameof(appFileMask));
            _outputDir = outputDir ??
                throw new ArgumentNullException(nameof(outputDir));
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

            command.Description = "Remove app overlaps from text documents " +
                "saving the updated documents into the specified directory.";
            command.HelpOption("-?|-h|--help");

            CommandArgument appDirArgument = command.Argument("[app-file-dir]",
                "The input apparatus files directory");
            CommandArgument appMaskArgument = command.Argument("[app-file-mask]",
                "The input apparatus files mask");
            CommandArgument outputDirArgument = command.Argument("[output-dir]",
                "The output directory");

            CommandOption regexMaskOption = command.Option("-r|--regex",
                "Use regular expressions in files masks", CommandOptionType.NoValue);
            CommandOption recursiveOption = command.Option("-s|--sub",
                "Recurse subdirectories in matching files masks",
                CommandOptionType.NoValue);

            command.OnExecute(() =>
            {
                options.Command = new RemoveOverlapsCommand(
                    appDirArgument.Value,
                    appMaskArgument.Value,
                    outputDirArgument.Value,
                    regexMaskOption.HasValue(),
                    recursiveOption.HasValue());
                return 0;
            });
        }

        private static bool IsFirstTarget(AppElemLocations a, AppElemLocations b)
        {
            // the widest range wins
            if (a.Locations.Length != b.Locations.Length)
            {
                return a.Locations.Length > b.Locations.Length;
            }

            // or the one with highest children wins
            int aChildCount = a.Element.Elements().Count();
            int bChildCount = b.Element.Elements().Count();
            if (aChildCount != bChildCount)
            {
                return aChildCount > bChildCount;
            }

            // or the first wins
            return true;
        }

        private static string GetAttributesDump(XElement element) =>
            string.Join(" ", from attr in element.Attributes()
                             select $"{attr.Name.LocalName}=\"{attr.Value}\"");

        private static bool IsAttrValueSubsetOf(string a, string b)
        {
            char[] sep = new[] { ' ' };
            HashSet<string> aTokens = new HashSet<string>(
                a.Split(sep, StringSplitOptions.RemoveEmptyEntries));
            HashSet<string> bTokens = new HashSet<string>(
                b.Split(sep, StringSplitOptions.RemoveEmptyEntries));
            return aTokens.IsSubsetOf(bTokens);
        }

        private static bool LemHasLostAttributes(XElement sourceLem,
            XElement targetLem)
        {
            // nothing lost if no source lem
            if (sourceLem == null) return false;

            // if no @wit/@source in source lem, nothing is lost
            string sourceW = sourceLem.Attribute("wit")?.Value;
            string sourceS = sourceLem.Attribute("source")?.Value;
            if (sourceW == null && sourceS == null) return false;

            // if no lem in target, any @wit/@source in lem is lost anyway
            if (targetLem == null) return true;

            string targetW = targetLem.Attribute("wit")?.Value;
            string targetS = targetLem.Attribute("source")?.Value;

            // lost if source lem @wit and no target lem @wit,
            // or source lem @source and no target lem @source
            if ((sourceW != null && targetW == null)
                || (sourceS != null && targetS == null))
            {
                return true;
            }

            // lost if source lem @wit is not a subset of target lem @wit
            if (sourceW != null && !IsAttrValueSubsetOf(sourceW, targetW))
                return false;

            // lost if source lem @source is not a subset of target lem @source
            if (sourceS != null && !IsAttrValueSubsetOf(sourceS, targetS))
                return false;

            // else not lost, because all the source attributes if any
            // are subsets of the target attributes
            return true;
        }

        public Task Run()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("REMOVE OVERLAPS\n");
            Console.ResetColor();
            Console.WriteLine(
                $"Input:  {_appFileMask}\n" +
                $"Output: {_outputDir}\n");

            int inputFileCount = 0;
            int removedCount = 0;

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(Log.Logger);
            Log.Logger.Information("REMOVE OVERLAPS");

            if (!Directory.Exists(_outputDir))
                Directory.CreateDirectory(_outputDir);

            // for each app document
            WordIdList widList = new WordIdList
            {
                Logger = loggerFactory.CreateLogger("report-overlaps")
            };
            foreach (string filePath in FileEnumerator.Enumerate(
                _appFileDir, _appFileMask, _regexMask, _recursive))
            {
                Console.WriteLine();
                Log.Logger.Information("Parsing {FilePath}", filePath);

                // load app document
                string inputFileName = Path.GetFileNameWithoutExtension(filePath);
                Console.WriteLine(filePath);
                inputFileCount++;
                XDocument doc = XDocument.Load(filePath,
                    LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

                // collect word IDs from text document
                widList.Parse(XDocument.Load(filePath.Replace("-app.", ".")));

                // collect app's locations
                List<AppElemLocations> appElemLocs =
                    AppElemLocationCollector.Collect(doc, widList,
                    AppElemLocationCollector.IsOverlappable);

                // detect and process overlaps
                for (int i = 0; i < appElemLocs.Count - 1; i++)
                {
                    for (int j = i + 1; j < appElemLocs.Count; j++)
                    {
                        if (appElemLocs[i].Overlaps(appElemLocs[j]))
                        {
                            // pick the target between the two overlapping app's
                            AppElemLocations target, source;
                            int targetIndex, sourceIndex;

                            if (IsFirstTarget(appElemLocs[i], appElemLocs[j]))
                            {
                                target = appElemLocs[targetIndex = i];
                                source = appElemLocs[sourceIndex = j];
                            }
                            else
                            {
                                source = appElemLocs[sourceIndex = i];
                                target = appElemLocs[targetIndex = j];
                            }

                            Log.Logger.Information("Merging overlapping app " +
                                $"{GetAttributesDump(source.Element)} into " +
                                GetAttributesDump(target.Element));

                            // log error if the source had @wit/@source
                            if (LemHasLostAttributes(
                                source.Element.Element(XmlHelper.TEI + "lem"),
                                target.Element.Element(XmlHelper.TEI + "lem")))
                            {
                                Log.Logger.Error("Removed overlapping app lost sources at div "
                                    + source.Element.Ancestors(XmlHelper.TEI + "div1")
                                        .First()
                                        .Attribute(XmlHelper.XML + "id").Value
                                    + ": "
                                    + GetAttributesDump(source.Element));
                            }

                            // append content of source into target in XML,
                            // excluding the lem child, and adding @n to each child
                            string nValue =
                                source.Element.Attribute("from").Value.Substring(1)
                                + " "
                                + source.Element.Attribute("to").Value.Substring(1);
                            foreach (XElement child in source.Element.Elements()
                                .Where(e => e.Name.LocalName != "lem"))
                            {
                                child.SetAttributeValue("n", nValue);
                                target.Element.Add(child);
                            }

                            // remove source from XML and locs
                            source.Element.Remove();
                            appElemLocs.RemoveAt(sourceIndex);
                            removedCount++;

                            // continue looking from overlaps from the first
                            // of the two app's involved
                            i = Math.Min(sourceIndex, targetIndex) - 1;
                            goto nextOuter;
                        }
                    } // j
                nextOuter:
                    if (i % 10 == 0) Console.Write('.');
                } // i

                // save
                string path = Path.Combine(_outputDir, Path.GetFileName(filePath));
                doc.Save(path, SaveOptions.OmitDuplicateNamespaces);
            }

            Console.WriteLine($"\nInput documents: {inputFileCount}");
            Console.WriteLine($"Removed overlaps: {removedCount}");
            return Task.CompletedTask;
        }
    }
}
