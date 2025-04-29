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
        public XmlDocument Document { get; }
        public IComponentRenderContext Branch();
        public void AddDOMAction(string elementId, Action<IDOMNode> callback);
        public void RequestRerender();
    }
}
