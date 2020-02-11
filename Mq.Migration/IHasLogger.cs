using Microsoft.Extensions.Logging;

namespace Mq.Migration
{
    /// <summary>
    /// Interface implemented by objects wrapping an <see cref="ILogger"/>
    /// implementation.
    /// </summary>
    public interface IHasLogger
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        ILogger Logger { get; set; }
    }
}
