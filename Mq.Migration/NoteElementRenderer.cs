using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// XML note element(s) renderer. This is used by exporters to render
    /// a note text into 1 or more XML elements, keeping into account Markdown
    /// markers for bold and italic, and newlines.
    /// </summary>
    /// <seealso cref="Mq.Migration.IHasLogger" />
    public sealed class NoteElementRenderer : IHasLogger
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        private void RenderText(string text, XElement target)
        {
            // bold = __...__
            // italic = _..._
            // (CR)LF = lb
            StringBuilder sb = new StringBuilder();
            int i = 0;
            Stack<char> emphStack = new Stack<char>();
            XElement current = target;

            while (i < text.Length)
            {
                switch (text[i])
                {
                    case '_':
                        if (sb.Length > 0)
                        {
                            current.Add(sb.ToString());
                            sb.Clear();
                        }
                        // __ = bold
                        if (i + 1 < text.Length && text[i + 1] == '_')
                        {
                            // closing
                            if (emphStack.Count > 0 && emphStack.Peek() == 'b')
                            {
                                emphStack.Pop();
                                current = current.Parent;
                            }
                            // opening
                            else
                            {
                                XElement emph = new XElement(XmlHelper.TEI + "emph",
                                    new XAttribute("style", "font-weight:bold"));
                                current.Add(emph);
                                current = emph;
                                emphStack.Push('b');
                            }
                            i += 2;
                        }
                        // _ = italic
                        else
                        {
                            // closing
                            if (emphStack.Count > 0 && emphStack.Peek() == 'i')
                            {
                                emphStack.Pop();
                                current = current.Parent;
                            }
                            // opening
                            else
                            {
                                XElement emph = new XElement(XmlHelper.TEI + "emph",
                                    new XAttribute("style", "font-style:italic"));
                                current.Add(emph);
                                current = emph;
                                emphStack.Push('i');
                            }
                            i++;
                        }
                        break;

                    case '\r':
                        if (i + 1 < text.Length && text[i + 1] == '\n')
                        {
                            i++;
                            break;
                        }
                        goto case '\n';
                    case '\n':
                        if (sb.Length > 0)
                        {
                            current.Add(sb.ToString());
                            sb.Clear();
                        }
                        if (current != target) current = target;
                        current.Add(XmlHelper.TEI + "lb");
                        i++;
                        break;

                    default:
                        sb.Append(text[i++]);
                        break;
                }
            }
            if (sb.Length > 0) current.Add(sb.ToString());
        }

        /// <summary>
        /// Renders the specified note's text.
        /// </summary>
        /// <param name="text">The note's text.</param>
        /// <param name="target">The optional target ID. When specified,
        /// it will be added to the <c>target</c> attribute.</param>
        /// <returns>Note element(s).</returns>
        /// <exception cref="ArgumentNullException">text</exception>
        public IList<XElement> Render(string text, string target = null)
        {
            if (text == null) throw new ArgumentNullException(nameof(text));

            List<XElement> noteElems = new List<XElement>();
            string[] sections = text.Split('`');

            for (int i = 0; i < sections.Length; i++)
            {
                if (sections[i].Length == 0) continue;

                // add or note with @type
                XElement noteElem = null;

                switch (i + 1)
                {
                    case 1:
                        noteElem = new XElement(XmlHelper.TEI + "add",
                            new XAttribute("type", "abstract"));
                        break;
                    case 2:
                        noteElem = new XElement(XmlHelper.TEI + "note",
                            new XAttribute("type", "operation"));
                        break;
                    case 3:
                        noteElem = new XElement(XmlHelper.TEI + "note",
                            new XAttribute("type", "details"));
                        break;
                    case 4:
                        noteElem = new XElement(XmlHelper.TEI + "add",
                            new XAttribute("type", "intertext"));
                        break;
                    default:
                        Logger?.LogError($"Unexpected note section #{i + 1}");
                        break;
                }
                if (noteElem == null) continue;

                // [@target]
                if (target != null) noteElem.SetAttributeValue("target", target);

                // text value
                RenderText(sections[i], noteElem);

                if (noteElem.Value.Length > 0) noteElems.Add(noteElem);
            }
            return noteElems;
        }
    }
}
