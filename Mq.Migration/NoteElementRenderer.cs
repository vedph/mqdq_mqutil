using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Mq.Migration
{
    /// <summary>
    /// XML note element(s) renderer. This is used by exporters to render
    /// a note text into 1 or more XML elements.
    /// </summary>
    /// <seealso cref="Mq.Migration.IHasLogger" />
    public sealed class NoteElementRenderer : IHasLogger
    {
        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        public ILogger Logger { get; set; }

        private string RenderText(string text)
        {
            // bold = __...__
            // italic = _..._
            // (CR)LF = lb
            StringBuilder sb = new StringBuilder(text);
            int i = 0;
            Stack<char> emphStack = new Stack<char>();

            while (i < text.Length)
            {
                switch (text[i])
                {
                    case '_':
                        // __ = bold
                        if (i + 1 < text.Length && text[i + 1] == '_')
                        {
                            if (emphStack.Peek() == 'b')
                            {
                                emphStack.Pop();
                                sb.Append("</emph>");
                            }
                            else
                            {
                                emphStack.Push('b');
                                sb.Append("<emph style=\"font-weight:bold\">");
                            }
                            i += 2;
                        }
                        // _ = italic
                        else
                        {
                            if (emphStack.Peek() == 'i')
                            {
                                emphStack.Pop();
                                sb.Append("</emph>");
                            }
                            else
                            {
                                emphStack.Push('i');
                                sb.Append("<emph style=\"font-style:italic\">");
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
                        sb.AppendLine("<br />");
                        i++;
                        break;
                    default:
                        sb.Append(text[i++]);
                        break;
                }
            }
            return sb.ToString();
        }

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
                noteElem.Value = RenderText(sections[i]);
                if (noteElem.Value.Length > 0) noteElems.Add(noteElem);
            }
            return noteElems;
        }
    }
}
