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

        private string GetFormatTagName(TextStyle style)
        {
            return style switch
            {
                TextStyle.Bold => "b",
                TextStyle.Italic => "i",
                TextStyle.Underline => "u",
                TextStyle.Strikethrough => "s",
                TextStyle.Superscript => "sup",
                TextStyle.Subscript => "sub",
                _ => throw new NotImplementedException()
            };
        }

        private IEnumerable<TextStyle> GetStyles(TextStyle flags)
        {
            // get individual style components from bitwise flags
            var testFlag = TextStyle.Superscript;
            while (testFlag > 0)
            {
                if ((flags & testFlag) > 0)
                    yield return testFlag;
                testFlag = (TextStyle)((int)testFlag << 1);
            }
        }
    }

    public class FormattedTextNode
    {
        public FormattedTextNode? Previous { get; }
        public FormattedTextNode? Next { get; }
        public string Text { get; }
        public TextStyle Style { get; }
        public CSSColor? Color { get; }

        public FormattedTextNode(string text, TextStyle style, CSSColor? color = null)
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
    public enum TextStyle
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
