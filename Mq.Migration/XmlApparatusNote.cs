namespace Mq.Migration
{
    /// <summary>
    /// A note being parsed from an apparatus XML document.
    /// </summary>
    public sealed class XmlApparatusNote
    {
        /// <summary>
        /// Gets or sets the note's section identifier (1-4).
        /// </summary>
        public int SectionId { get; set; }

        /// <summary>
        /// Gets or sets the optional note target.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the note's text value.
        /// </summary>
        public string Value { get; set; }
    }
}
