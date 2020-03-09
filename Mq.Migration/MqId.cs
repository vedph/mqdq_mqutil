using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Mq.Migration
{
    /// <summary>
    /// MQDQ word ID.
    /// </summary>
    public sealed class MqId
    {
        /// <summary>
        /// Gets the div number.
        /// </summary>
        public int Div { get; }

        /// <summary>
        /// Gets the word number.
        /// </summary>
        public int Word { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MqId"/> class.
        /// </summary>
        /// <param name="div">The div number.</param>
        /// <param name="word">The word number.</param>
        public MqId(int div, int word)
        {
            Div = div;
            Word = word;
        }

        /// <summary>
        /// Compares this ID to another ID.
        /// </summary>
        /// <param name="other">The other ID.</param>
        /// <returns>0 if equal, less than 0 if this is less than the other,
        /// greater than 0 if this is greater than the other.</returns>
        public int CompareTo(MqId other)
        {
            if (other == null) return 1;
            if (Div != other.Div) return Div - other.Div;
            return Word - other.Word;
        }

        /// <summary>
        /// Determines whether this ID is inside the specified range.
        /// </summary>
        /// <param name="from">From ID.</param>
        /// <param name="to">To ID.</param>
        /// <returns><c>true</c> if inside; otherwise, <c>false</c>.</returns>
        public bool IsInside(MqId from, MqId to)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));

            return CompareTo(from) >= 0 && CompareTo(to) <= 0;
        }

        /// <summary>
        /// Parses the specified ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>ID or null if invalid text.</returns>
        /// <exception cref="ArgumentNullException">id</exception>
        static public MqId Parse(string id)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            Match m = Regex.Match(id, @"\#?d(\d+)w(\d+)");
            if (!m.Success) return null;
            return new MqId(
                int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"d{Div}w{Word}";
        }
    }
}
