using LibChromeDotNet.CDP.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public class DOMNode : IDOMNode
    {
        private IInteropSession _Session;
        private DOMNodeTree _NodeTree;
        private DOMNodeInfo _NodeInfo => _NodeTree.Node;

        public DOMNode(IInteropSession session, DOMNodeTree tree)
        {
            _Session = session;
            _NodeTree = tree;
        }

        public string Name => _NodeInfo.Name;

        public async Task DeleteNodeAsync()
        {
            await _Session.RequestAsync(DOM.RemoveNode(_NodeInfo.Id));
        }

        public async Task<IEnumerable<KeyValuePair<string, string>>> GetAttributesAsync()
        {
            return await _Session.RequestAsync(DOM.GetAttributes(_NodeInfo.Id));
        }

        public async Task<IEnumerable<IDOMNode>> GetChildrenAsync()
        {
            if (_NodeTree.Depth == 0)
                _NodeTree = await _Session.RequestAsync(DOM.GetNodeTree(_NodeInfo.Id, 2));
            if (_NodeTree.Children == null)
                return Enumerable.Empty<IDOMNode>();
            return _NodeTree.Children
                .Select(n => new DOMNode(_Session, n));
        }

        public async Task<IJSObject> GetJavascriptNodeAsync()
        {
            var remoteObject = await _Session.RequestAsync(DOM.ResolveJavscriptNode(_NodeInfo.Id));
            return new JSObject(_Session, remoteObject);
        }

        public async Task<IDOMNode> QuerySelectAsync(string selector)
        {
            var resultId = await _Session.RequestAsync(DOM.QuerySelector(_NodeInfo.Id, selector));
            var resultTree = await _Session.RequestAsync(DOM.GetNodeTree(resultId, 2));
            return new DOMNode(_Session, resultTree);
        }

        public async Task<IDOMNode[]> QuerySelectManyAsync(string selector)
        {
            var resultIds = await _Session.RequestAsync(DOM.QuerySelectorMany(_NodeInfo.Id, selector));
            List<IDOMNode> results = new List<IDOMNode>();
            foreach (var resultId in resultIds)
            {
                var nodeTree = await _Session.RequestAsync(DOM.GetNodeTree(resultId, 1));
                results.Add(new DOMNode(_Session, nodeTree));
            }
            return results.ToArray();
        }

        public Task RemoveAttributeAsync(string attrName)
        {
            throw new NotImplementedException();
        }

        public Task SetAttributeAsync(string attrName, string newValue)
        {
            throw new NotImplementedException();
        }

        public Task SetValueAsync(string value)
        {
            throw new NotImplementedException();
        }
    }
}
