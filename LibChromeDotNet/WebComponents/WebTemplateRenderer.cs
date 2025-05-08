using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.WebComponents.ILGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LibChromeDotNet.WebComponents
{
    public static class WebTemplateRenderer
    {
        public static async Task RenderTemplateAsync(this IDOMNode node, SubstituterInfo template, IComponentResource resource)
        {
            var ctx = new ComponentRenderContext(node, template, resource);
            await ctx.RequestRerenderAsync();
        }

        private class ComponentRenderContext : IComponentRenderContext
        {
            public XmlDocument Document => _Document!;

            public ComponentRenderContext(IDOMNode node, SubstituterInfo substituter, IComponentResource resource)
            {
                _Node = node;
                _Substituter = substituter;
                _Resource = resource;
            }

            private IDOMNode _Node;
            private XmlDocument? _Document;
            private SubstituterInfo _Substituter;
            private IComponentResource _Resource;
            private List<KeyValuePair<string, Action<IDOMNode>>>? _DOMActionList;

            public void AddDOMAction(string elementId, Action<IDOMNode> callback)
            {
                _DOMActionList!.Add(new KeyValuePair<string, Action<IDOMNode>>(elementId, callback));
            }

            public async Task RequestRerenderAsync()
            {
                _Document = new XmlDocument();
                _DOMActionList = new List<KeyValuePair<string, Action<IDOMNode>>>();
                _Substituter.Substituter(this, _Resource);
                await _Node.ModifyOuterHTMLAsync(_Document);
                foreach (var domActionPair in _DOMActionList)
                {
                    var requestedNode = await _Node.QuerySelectAsync($"#{domActionPair.Key}");
                    if (requestedNode != null)
                        domActionPair.Value(requestedNode);
                }
            }
        }
    }
}
