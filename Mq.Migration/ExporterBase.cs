using Cadmus.Core.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// Base class for exporters.
    /// </summary>
    public abstract class ExporterBase : IHasLogger
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include some comments
        /// in the output XML. These can be used for diagnostic purposes.
        /// </summary>
        public bool IncludeComments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether dry mode is enabled.
        /// </summary>
        public bool IsDryModeEnabled { get; set; }

        /// <summary>
        /// Gets the repository.
        /// </summary>
        protected ICadmusRepository Repository { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExporterBase"/> class.
        /// </summary>
        /// <param name="repository">The source repository.</param>
        /// <exception cref="ArgumentNullException">repository</exception>
        protected ExporterBase(ICadmusRepository repository)
        {
            Repository = repository ??
                throw new ArgumentNullException(nameof(repository));
        }

        protected static string GetGroupIdDirectoryName(string groupId)
        {
            int i = groupId.IndexOf('-');
            return i == -1 ? null : groupId.Substring(0, i);
        }

        /// <summary>
        /// Clears the div contents by removing all the children elements
        /// except for the initial <c>head</c> and any other <c>div</c>'s.
        /// </summary>
        /// <param name="div">The div element to be cleared.</param>
        protected void ClearDivContents(XElement div)
        {
            XElement head = div.Elements(XmlHelper.TEI + "head").FirstOrDefault();
            if (head != null)
            {
                if (head.ElementsBeforeSelf().Any())
                {
                    Logger?.LogError(
                        $"Unexpected elements before head in {div.Name.LocalName}"
                        + " at " + XmlHelper.GetLineInfo(div));
                }
                foreach (XElement sibling in head.ElementsAfterSelf()
                    .Where(e => !e.Name.LocalName.StartsWith("div")).ToList())
                {
                    sibling.Remove();
                }
            }
            else
            {
                foreach (XElement child in div.Elements()
                    .Where(e => !e.Name.LocalName.StartsWith("div")).ToList())
                {
                    child.Remove();
                }
            }
        }
    }
}
