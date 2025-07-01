using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5.DOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LibChromeDotNet.WebComponents.ILGen
{
    public interface IComponentRenderContext
    {
        public IElementRenderer CreateElement(bool root);
    }

    public interface IElementRenderer
    {
        public string Name { get; set; }
        public string InnerText { get; set; }
        public void SetAttribute(string name, string value);
        public void SetChildren(IEnumerable<IElementRenderer> children);
        public void AddDOMInitializer(Action<IDOMNode> callback);
    }
}
