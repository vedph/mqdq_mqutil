using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// MQDQ text document partitioner.
    /// </summary>
    public sealed class XmlPartitioner
    {
        private readonly HashSet<string> _applicableTypes;

        private readonly Regex _breakRegex;
        private XElement _lastBreakPoint;
        private bool _exceeded;
        private string _docId;
        private int _minTreshold;
        private int _maxTreshold;

        #region Properties
        /// <summary>
        /// Gets or sets the minimum treshold.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">value</exception>
        public int MinTreshold
        {
            get { return _minTreshold; }
            set
            {
                if (value < 1 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _minTreshold = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum treshold.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">value</exception>
        public int MaxTreshold
        {
            get { return _maxTreshold; }
            set
            {
                if (value < 1 || value > 1000)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _maxTreshold = value;
            }
        }

        public static string TEI { get; set; }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlPartitioner"/> class.
        /// </summary>
        public XmlPartitioner()
        {
            _breakRegex = new Regex(@"[\u037e.?!][^\p{L}]*$");
            MinTreshold = 20;
            MaxTreshold = 50;
            _applicableTypes = new HashSet<string> { "section", "work" };
        }

        /// <summary>
        /// Determines whether the partitioner is applicable to the specified
        /// document.
        /// </summary>
        /// <param name="doc">The document to test.</param>
        /// <returns>
        ///   <c>true</c> if the specified document is applicable; otherwise,
        ///   <c>false</c>.
        /// </returns>
        /// <remarks>To be applicable, a document must contain at least one
        /// <c>div1</c> of <c>type</c> <c>section</c> or <c>work</c>, whose
        /// <c>l</c> children count is greater than <see cref="MaxTreshold"/>;
        /// also, it should contain no <c>div2</c> at all.
        /// </remarks>
        /// <exception cref="ArgumentNullException">doc</exception>
        public bool IsApplicable(XDocument doc)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            XElement body = XmlHelper.GetTeiBody(doc);

            return !body.Descendants(XmlHelper.TEI + "div2").Any()
                && body.Elements(XmlHelper.TEI + "div1")
                    .Any(e => _applicableTypes.Contains(e.Attribute("type")?.Value)
                         && e.Elements(XmlHelper.TEI + "l").Count() > MaxTreshold);
        }

        private bool IsBreakPoint(XElement l, int ordinal)
        {
            if (_breakRegex.IsMatch(l.Value))
            {
                _lastBreakPoint = l;
                if (ordinal >= MinTreshold) return true;
            }
            if (ordinal >= MaxTreshold)
            {
                _exceeded = true;
                return true;
            }
            return false;
        }

        private void InsertBreakPast(XElement l)
        {
            string n = XmlHelper.GetBreakPointCitation(0, l, _docId);
            l.AddAfterSelf(new XElement(XmlHelper.TEI + "pb",
                new XAttribute("n", n)));
        }

        /// <summary>
        /// Partitions the specified document by adding breakpoints to it.
        /// </summary>
        /// <param name="doc">The XML document.</param>
        /// <param name="id">The document ID (=its filename, no extension).</param>
        /// <returns>True if document was touched, else false.</returns>
        /// <exception cref="ArgumentNullException">doc or id</exception>
        /// <exception cref="InvalidOperationException">min treshold greater
        /// than max</exception>
        public bool Partition(XDocument doc, string id)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (id == null) throw new ArgumentNullException(nameof(id));

            if (_minTreshold > _maxTreshold)
                throw new InvalidOperationException("Min treshold > max");

            // do nothing if partitioning is not required
            if (!IsApplicable(doc)) return false;

            _docId = id;
            bool touched = false;

            // examine each div1 requiring partitioning
            foreach (XElement div1 in XmlHelper.GetTeiBody(doc)
                .Descendants(XmlHelper.TEI + "div1")
                .Where(e => _applicableTypes.Contains(e.Attribute("type")?.Value)
                            && e.Elements(XmlHelper.TEI + "l").Count() > MaxTreshold)
                .ToList())
            {
                // the l to start the partition from
                XElement firstL = div1.Element(XmlHelper.TEI + "l");

                // keep partitioning until we get to the div1's end
                while (firstL != null)
                {
                    // reset state for the current partition
                    _lastBreakPoint = null;
                    _exceeded = false;

                    // reach the last l in the partition
                    XElement lastL = firstL
                        .ElementsAfterSelf(XmlHelper.TEI + "l")
                        .TakeWhile((e, i) => !IsBreakPoint(e, i + 2))
                        .LastOrDefault();

                    // corner case: no last l, i.e. the first l considered is
                    // also the last one.
                    // In this case, move it under the previous partition if any.
                    if (lastL == null)
                    {
                        XElement pb = firstL.ElementsBeforeSelf(XmlHelper.TEI + "pb")
                            .FirstOrDefault();
                        if (pb != null)
                        {
                            firstL.Remove();
                            pb.AddBeforeSelf(firstL);
                            touched = true;
                        }
                        break;
                    }
                    XElement pastLastL = lastL.ElementsAfterSelf(XmlHelper.TEI + "l")?
                        .FirstOrDefault();
                    lastL = pastLastL ?? lastL;

                    // corner case: stopped for excess, but a previous
                    // break point candidate was dropped because below min treshold
                    if (_exceeded && _lastBreakPoint != null)
                    {
                        InsertBreakPast(_lastBreakPoint);
                        firstL = _lastBreakPoint.ElementsAfterSelf(XmlHelper.TEI + "l")
                            .FirstOrDefault();
                        touched = true;
                    }
                    else
                    {
                        // else just break past last l and continue from the next one
                        InsertBreakPast(lastL);
                        firstL = lastL.ElementsAfterSelf(XmlHelper.TEI + "l").FirstOrDefault();
                        touched = true;
                    }
                }
            }
            return touched;
        }
    }
}
