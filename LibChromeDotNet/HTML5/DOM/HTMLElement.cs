using LibChromeDotNet.CDP.Domains;
using LibChromeDotNet.ChromeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.DOM
{
    // This class and IHTMLElement provide an interface to wrap any IDOMNode which is of element type to provide
    // bindings to JS Web APIs which are specialized for different types of HTML elements. Implementations of
    // IHTMLElement should also bind to the update events specific to the element, to track attributes locally
    // which prevents pulling an unchanged value from chrome, and means the properties are ready as soon as they
    // are requested from C# APIs
    public static class HTMLElement
    {
        private static Dictionary<string, Func<IDOMNode, IHTMLElement>> _MappingDict = new Dictionary<string, Func<IDOMNode, IHTMLElement>>();

        public static void AddMapping(Func<IDOMNode, IHTMLElement> elementFactory, params string[] elementTypeNames)
        {
            foreach (var elementTypeName in elementTypeNames)
                _MappingDict.Add(elementTypeName, elementFactory);
        }

        public static IHTMLElement FromNode(IDOMNode node)
        {
            if (node.NodeType != DOMNodeType.Element && node.NodeType != DOMNodeType.Document)
                throw new ArgumentException($"{nameof(node)} is not an element node");
            if (!_MappingDict.TryGetValue(node.Name, out var factory))
                throw new ArgumentException($"The element type '{node.Name}' is unsupported");
            return factory(node);
        }

        public static TElement FromNode<TElement>(IDOMNode node)
        {
            var element = FromNode(node);
            if (element is not TElement result)
                throw new ArgumentException($"{nameof(node)} did not map to type {nameof(TElement)}");
            return result;
        }
    }
}
