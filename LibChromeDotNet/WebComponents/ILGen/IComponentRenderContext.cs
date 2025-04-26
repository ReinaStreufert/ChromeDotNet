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
        public IWebTemplateSet TemplateSet { get; }
        public void SetEventListener(string elementId, GenericDOMEvent eventType, Action handler);
        public void SetEventListener<TParams>(string elementId, IDOMEvent<TParams> eventType, Action<TParams> handler);
        public Substituter? LoadDependencySubstituter(string name);
    }
}
