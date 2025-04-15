using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LibChromeDotNet.HTML5.CSS
{
    // generates html with inline css styles for multi-style text
    public class FormattedText
    {
        public FormattedTextNode FirstNode { get; }

        public FormattedText(FormattedTextNode firstNode)
        {
            if (firstNode.Previous != null)
                throw new ArgumentException($"{nameof(firstNode)} is not the first node");
            FirstNode = firstNode;
        }

        public void WriteToTag(XmlDocument xmlDst)
        {
            var node = FirstNode;
            do
            {
                Add(xmlDst, node);
                node = node.Next;
            } while (node != null);
        }

        private void Add(XmlDocument xmlDst, FormattedTextNode node)
        {
            XmlElement outerElement = xmlDst.DocumentElement!;
            if (node.Color != null)
            {
                var cssStyleAttribute = xmlDst.CreateAttribute("style");
                cssStyleAttribute.Value = $"color: {node.Color.Name};";
                var spanElement = xmlDst.CreateElement("span");
                spanElement.Attributes.Append(cssStyleAttribute);
                outerElement.AppendChild(spanElement);
                outerElement = spanElement;
            }
            foreach (var style in GetStyles(node.Style))
            {
                var styleElement = xmlDst.CreateElement(GetFormatTagName(style));
                outerElement.AppendChild(styleElement);
                outerElement = styleElement;
            }
            outerElement.InnerText = node.Text;
        }

        private string GetFormatTagName(FontStyle style)
        {
            return style switch
            {
                FontStyle.Bold => "b",
                FontStyle.Italic => "i",
                FontStyle.Underline => "u",
                FontStyle.Strikethrough => "s",
                FontStyle.Superscript => "sup",
                FontStyle.Subscript => "sub",
                _ => throw new NotImplementedException()
            };
        }

        private IEnumerable<FontStyle> GetStyles(FontStyle flags)
        {
            // get individual style components from bitwise flags
            var testFlag = FontStyle.Superscript;
            while ((int)testFlag > 0)
            {
                if ((flags & testFlag) > 0)
                    yield return testFlag;
                testFlag = (FontStyle)((int)testFlag >> 1);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            FormattedTextNode? node = FirstNode;
            while (node != null)
            {
                sb.Append(node.Text);
                node = node.Next;
            }
            return sb.ToString();
        }
    }

    public class FormattedTextNode
    {
        public FormattedTextNode? Previous => _Previous;
        public FormattedTextNode? Next => _Next;
        public string Text { get; }
        public FontStyle Style { get; }
        public ICSSColor? Color { get; }

        public FormattedTextNode(string text, FontStyle style, ICSSColor? color = null)
        {
            Text = text;
            Style = style;
            Color = color;
        }

        private FormattedTextNode? _Previous;
        private FormattedTextNode? _Next;

        public void InsertAfter(FormattedTextNode node)
        {
            node._Next = _Next;
            _Next = node;
            node._Previous = this;
        }

        public void InsertBefore(FormattedTextNode node)
        {
            node._Previous = _Previous;
            _Previous = node;
            node._Next = this;
        }

        public void DeleteAfter()
        {
            if (_Next == null)
                throw new InvalidOperationException("There is no node after this node");
            _Next = _Next._Next;
        }

        public void DeleteBefore()
        {
            if (_Previous == null)
                throw new InvalidOperationException("There is no node before this node");
            _Previous = _Previous._Previous;
        }
    }

    [Flags]
    public enum FontStyle
    {
        Regular = 0,
        Bold = 1,
        Italic = 2,
        Underline = 4,
        Strikethrough = 8,
        Subscript = 16,
        Superscript = 32
    }
}
