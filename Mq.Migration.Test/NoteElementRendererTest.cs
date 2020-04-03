using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;

namespace Mq.Migration.Test
{
    public sealed class NoteElementRendererTest
    {
        private const string TEI_NS = "http://www.tei-c.org/ns/1.0";

        [Fact]
        public void Render_Empty_None()
        {
            NoteElementRenderer renderer = new NoteElementRenderer();

            IList<XElement> elements = renderer.Render("");

            Assert.Empty(elements);
        }

        [Fact]
        public void Render_NoSeparator_AddAbstract()
        {
            NoteElementRenderer renderer = new NoteElementRenderer();

            IList<XElement> elements = renderer.Render("section 1");

            Assert.Single(elements);
            // 1: add @type=abstract
            XElement elem = elements[0];
            Assert.Equal(XmlHelper.TEI + "add", elem.Name);
            Assert.Equal("abstract", elem.Attribute("type")?.Value);
            Assert.Equal("section 1", elem.Value);
        }

        [Fact]
        public void Render_NoSeparatorWithTarget_AddAbstract()
        {
            NoteElementRenderer renderer = new NoteElementRenderer();

            IList<XElement> elements = renderer.Render("section 1", "#lb1-50");

            Assert.Single(elements);
            // 1: add @type=abstract @target=#lb1-50
            XElement elem = elements[0];
            Assert.Equal(XmlHelper.TEI + "add", elem.Name);
            Assert.Equal("abstract", elem.Attribute("type")?.Value);
            Assert.Equal("#lb1-50", elem.Attribute("target")?.Value);
            Assert.Equal("section 1", elem.Value);
        }

        [Fact]
        public void Render_1Separator_AddAbstractNoteOperation()
        {
            NoteElementRenderer renderer = new NoteElementRenderer();

            IList<XElement> elements = renderer.Render("section 1`section 2");

            Assert.Equal(2, elements.Count);
            // 1: add @type=abstract
            XElement elem = elements[0];
            Assert.Equal(XmlHelper.TEI + "add", elem.Name);
            Assert.Equal("abstract", elem.Attribute("type")?.Value);
            Assert.Equal("section 1", elem.Value);

            // 2: note @type=operation
            elem = elements[1];
            Assert.Equal(XmlHelper.TEI + "note", elem.Name);
            Assert.Equal("operation", elem.Attribute("type")?.Value);
            Assert.Equal("section 2", elem.Value);
        }

        [Fact]
        public void Render_InitialSeparator_NoteOperation()
        {
            NoteElementRenderer renderer = new NoteElementRenderer();

            IList<XElement> elements = renderer.Render("`section 2");

            Assert.Single(elements);
            // 2: note @type=operation
            XElement elem = elements[0];
            Assert.Equal(XmlHelper.TEI + "note", elem.Name);
            Assert.Equal("operation", elem.Attribute("type")?.Value);
            Assert.Equal("section 2", elem.Value);
        }

        [Fact]
        public void Render_2Separators_AddAbstractNoteOperationNoteDetails()
        {
            NoteElementRenderer renderer = new NoteElementRenderer();

            IList<XElement> elements = renderer.Render("section 1`section 2`section 3");

            Assert.Equal(3, elements.Count);
            // 1: add @type=abstract
            XElement elem = elements[0];
            Assert.Equal(XmlHelper.TEI + "add", elem.Name);
            Assert.Equal("abstract", elem.Attribute("type")?.Value);
            Assert.Equal("section 1", elem.Value);

            // 2: note @type=operation
            elem = elements[1];
            Assert.Equal(XmlHelper.TEI + "note", elem.Name);
            Assert.Equal("operation", elem.Attribute("type")?.Value);
            Assert.Equal("section 2", elem.Value);

            // 3: note @type=details
            elem = elements[2];
            Assert.Equal(XmlHelper.TEI + "note", elem.Name);
            Assert.Equal("details", elem.Attribute("type")?.Value);
            Assert.Equal("section 3", elem.Value);
        }

        [Fact]
        public void Render_3Separators_AddAbstractNoteOperationNoteDetailsAddIntertext()
        {
            NoteElementRenderer renderer = new NoteElementRenderer();

            IList<XElement> elements = renderer.Render("section 1`section 2" +
                "`section 3`section 4");

            Assert.Equal(4, elements.Count);
            // 1: add @type=abstract
            XElement elem = elements[0];
            Assert.Equal(XmlHelper.TEI + "add", elem.Name);
            Assert.Equal("abstract", elem.Attribute("type")?.Value);
            Assert.Equal("section 1", elem.Value);

            // 2: note @type=operation
            elem = elements[1];
            Assert.Equal(XmlHelper.TEI + "note", elem.Name);
            Assert.Equal("operation", elem.Attribute("type")?.Value);
            Assert.Equal("section 2", elem.Value);

            // 3: note @type=details
            elem = elements[2];
            Assert.Equal(XmlHelper.TEI + "note", elem.Name);
            Assert.Equal("details", elem.Attribute("type")?.Value);
            Assert.Equal("section 3", elem.Value);

            // 3: add @type=intertext
            elem = elements[3];
            Assert.Equal(XmlHelper.TEI + "add", elem.Name);
            Assert.Equal("intertext", elem.Attribute("type")?.Value);
            Assert.Equal("section 4", elem.Value);
        }

        [Fact]
        public void Render_Bold_Emph()
        {
            NoteElementRenderer renderer = new NoteElementRenderer();

            IList<XElement> elements = renderer.Render("hello __world__!");

            Assert.Single(elements);
            // 1: add @type=abstract
            XElement elem = elements[0];
            Assert.Equal(XmlHelper.TEI + "add", elem.Name);
            Assert.Equal("abstract", elem.Attribute("type")?.Value);
            Assert.True(elem.HasElements);
            Assert.Equal("<add type=\"abstract\" " +
                $"xmlns=\"{TEI_NS}\">" +
                "hello <emph style=\"font-weight:bold\">world</emph>!</add>",
                elem.ToString());
        }

        [Fact]
        public void Render_Italic_Emph()
        {
            NoteElementRenderer renderer = new NoteElementRenderer();

            IList<XElement> elements = renderer.Render("hello _world_!");

            Assert.Single(elements);
            // 1: add @type=abstract
            XElement elem = elements[0];
            Assert.Equal(XmlHelper.TEI + "add", elem.Name);
            Assert.Equal("abstract", elem.Attribute("type")?.Value);
            Assert.True(elem.HasElements);
            Assert.Equal("<add type=\"abstract\" " +
                $"xmlns=\"{TEI_NS}\">" +
                "hello <emph style=\"font-style:italic\">world</emph>!</add>",
                elem.ToString());
        }

        [Fact]
        public void Render_Newline_Lb()
        {
            NoteElementRenderer renderer = new NoteElementRenderer();

            IList<XElement> elements = renderer.Render("hello\r\nworld!");

            Assert.Single(elements);
            // 1: add @type=abstract
            XElement elem = elements[0];
            Assert.Equal(XmlHelper.TEI + "add", elem.Name);
            Assert.Equal("abstract", elem.Attribute("type")?.Value);
            Assert.True(elem.HasElements);
            Assert.Equal("<add type=\"abstract\" " +
                $"xmlns=\"{TEI_NS}\">" +
                "hello<lb />world!</add>",
                elem.ToString());
        }

        [Fact]
        public void Render_BoldInItalic_NestedEmph()
        {
            NoteElementRenderer renderer = new NoteElementRenderer();

            IList<XElement> elements = renderer.Render("hello _my __world___!");

            Assert.Single(elements);
            // 1: add @type=abstract
            XElement elem = elements[0];
            Assert.Equal(XmlHelper.TEI + "add", elem.Name);
            Assert.Equal("abstract", elem.Attribute("type")?.Value);
            Assert.True(elem.HasElements);
            Assert.Equal("<add type=\"abstract\" " +
                $"xmlns=\"{TEI_NS}\">" +
                "hello <emph style=\"font-style:italic\">my " +
                "<emph style=\"font-weight:bold\">world</emph></emph>" +
                "!</add>",
                elem.ToString());
        }
    }
}
