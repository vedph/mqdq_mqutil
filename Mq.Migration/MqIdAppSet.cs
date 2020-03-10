using System;
using System.Collections.Generic;
using System.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// A set of MQDQ IDs for an app element. This is used for reporting
    /// overlaps independently from parsing apparatuses. Each set represents
    /// the words the app element refers to, via their IDs.
    /// </summary>
    /// <remarks>This assumes that IDs are continuous and ordered, which is
    /// not always the case; but it's enough for our reporting purpose.
    /// Otherwise, we should scan all the w elements in range and collect
    /// their IDs.
    /// </remarks>
    public sealed class MqIdAppSet
    {
        private MqId _from;
        private MqId _to;
        private List<MqId> _locs;

        /// <summary>
        /// Gets a value indicating whether this instance is a loc set instead
        /// of a from-to set.
        /// </summary>
        public bool IsLoc => _locs != null;

        /// <summary>
        /// Sets from and to IDs.
        /// </summary>
        /// <param name="from">From ID.</param>
        /// <param name="to">To ID.</param>
        /// <exception cref="ArgumentNullException">from or to</exception>
        public void SetFromTo(MqId from, MqId to)
        {
            _from = from ?? throw new ArgumentNullException(nameof(from));
            _to = to ?? throw new ArgumentNullException(nameof(to));

            _locs = null;
        }

        /// <summary>
        /// Sets the loc IDs.
        /// </summary>
        /// <param name="ids">The ids.</param>
        /// <exception cref="ArgumentNullException">ids</exception>
        public void SetLoc(IEnumerable<MqId> ids)
        {
            if (ids == null) throw new ArgumentNullException(nameof(ids));

            if (_locs == null) _locs = new List<MqId>();
            else _locs.Clear();
            _locs.AddRange(ids);

            _from = _to = null;
        }

        /// <summary>
        /// True if this set overlaps the specified other set.
        /// </summary>
        /// <param name="other">The other set.</param>
        /// <returns>True if overlap, else false.</returns>
        /// <exception cref="ArgumentNullException">other</exception>
        public bool Overlaps(MqIdAppSet other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            if (IsLoc)
            {
                if (other.IsLoc)
                {
                    // loc-loc
                    return _locs.Any(a => other._locs.Any(b => a.CompareTo(b) == 0));
                }
                else
                {
                    // loc-fromto
                    return _locs.Any(a => a.IsInside(other._from, other._to));
                }
            }
            else
            {
                if (other.IsLoc)
                {
                    // fromto-loc
                    return other._locs.Any(b => b.IsInside(_from, _to));
                }
                else
                {
                    // fromto-fromto
                    return _from.IsInside(other._from, other._to)
                        || _to.IsInside(other._from, other._to)
                        || other._from.IsInside(_from, _to)
                        || other._to.IsInside(_from, _to);
                }
            }
        }

        /// <summary>
        /// Gets the IDs in this set.
        /// </summary>
        /// <returns>List of IDs.</returns>
        public IList<MqId> GetIds()
        {
            if (IsLoc) return _locs;
            return new List<MqId>(new[] { _from, _to });
        }

        /// <summary>
        /// Converts to string.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return IsLoc
                ? string.Join(" ", _locs.Select(id => id.ToString()))
                : $"{_from}-{_to}";
        }
    }
}
