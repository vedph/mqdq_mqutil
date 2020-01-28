using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Mqutil.Xml
{
    /// <summary>
    /// MQDQ text document partitioner.
    /// </summary>
    public sealed class XmlPartitioner
    {
        public static readonly XNamespace TEI = "http://www.tei-c.org/ns/1.0";
        public static readonly XNamespace XML = "http://www.w3.org/XML/1998/namespace";

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

            XElement body = doc.Root
                .Element(TEI + "text")
                .Element(TEI + "body");

            return !body.Descendants(TEI + "div2").Any()
                && body.Elements(TEI + "div1")
                    .Any(e => _applicableTypes.Contains(e.Attribute("type")?.Value)
                         && e.Elements(TEI + "l").Count() > MaxTreshold);
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

        private string GetAttributeName(XAttribute a) =>
            (a.Name.Namespace == XML ? "xml:" : "") + a.Name.LocalName;

        private string ConcatDivAttributes(XElement div) =>
            string.Join("\u2016",
                div.Attributes().Select(a => $"{GetAttributeName(a)}={a.Value}"));

        private string GetBreakPointCitation(XElement l)
        {
            StringBuilder sb = new StringBuilder();

            // filename
            sb.Append(_docId);

            // div1 attrs
            XElement div1 = l.Ancestors(TEI + "div1").First();
            sb.Append(' ').Append(ConcatDivAttributes(div1));

            // div2 attrs if any
            XElement div2 = div1.Descendants(TEI + "div2").LastOrDefault();
            if (div2 != null)
            {
                sb.Append(' ').Append(ConcatDivAttributes(div2));
            }

            // line number # line ID
            sb.Append(' ')
              .Append(l.Attribute("n").Value)
              .Append('#')
              .Append(l.Attribute(XML + "id").Value);

            return sb.ToString();
        }

        private void InsertBreakPast(XElement l)
        {
            string n = GetBreakPointCitation(l);
            l.AddAfterSelf(new XElement(TEI + "pb",
                new XAttribute("n", n)));
        }

        /// <summary>
        /// Partitions the specified document by adding breakpoints to it.
        /// </summary>
        /// <param name="doc">The XML document.</param>
        /// <param name="id">The document ID (=its filename, no extension).</param>
        /// <returns>True if document was touched, else false.</returns>
        /// <exception cref="ArgumentNullException">doc</exception>
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
            foreach (XElement div1 in doc.Root
                .Element(TEI + "text")
                .Element(TEI + "body")
                .Descendants(TEI + "div1")
                .Where(e => _applicableTypes.Contains(e.Attribute("type")?.Value)
                            && e.Elements(TEI + "l").Count() > MaxTreshold)
                .ToList())
            {
                // the l to start the partition from
                XElement firstL = div1.Element(TEI + "l");

                // keep partitioning until we get to the div1's end
                while (firstL != null)
                {
                    // reset state for the current partition
                    _lastBreakPoint = null;
                    _exceeded = false;

                    // reach the last l in the partition
                    XElement lastL = firstL
                        .ElementsAfterSelf(TEI + "l")
                        .TakeWhile((e, i) => !IsBreakPoint(e, i + 2))
                        .LastOrDefault();

                    // corner case: no last l, i.e. the first l considered is
                    // also the last one.
                    // In this case, move it under the previous partition if any.
                    if (lastL == null)
                    {
                        XElement pb = firstL.ElementsBeforeSelf(TEI + "pb")
                            .FirstOrDefault();
                        if (pb != null)
                        {
                            firstL.Remove();
                            pb.AddBeforeSelf(firstL);
                            touched = true;
                        }
                        break;
                    }
                    XElement pastLastL = lastL.ElementsAfterSelf(TEI + "l")?
                        .FirstOrDefault();
                    lastL = pastLastL ?? lastL;

                    // corner case: stopped for excess, but a previous
                    // break point candidate was dropped because below min treshold
                    if (_exceeded && _lastBreakPoint != null)
                    {
                        InsertBreakPast(_lastBreakPoint);
                        firstL = _lastBreakPoint.ElementsAfterSelf(TEI + "l")
                            .FirstOrDefault();
                        touched = true;
                    }
                    else
                    {
                        // else just break past last l and continue from the next one
                        InsertBreakPast(lastL);
                        firstL = lastL.ElementsAfterSelf(TEI + "l").FirstOrDefault();
                        touched = true;
                    }
                }
            }
            return touched;
        }
    }
}
