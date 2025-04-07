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
        private ref DOMNodeInfo _NodeInfo => ref _NodeTree.Node;

        public DOMNode(IInteropSession session, DOMNodeTree tree)
        {
            _Session = session;
            _NodeTree = tree;
        }

        public string Name => _NodeInfo.Name;
        public DOMNodeType NodeType => _NodeInfo.NodeType;
        public IInteropSession Session => _Session;

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
            return new JSObject(_Session, remoteObject.ObjectId!);
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

        public async Task RemoveAttributeAsync(string attrName) =>
            await _Session.RequestAsync(DOM.RemoveAttribute(_NodeInfo.Id, attrName));
        public async Task SetAttributeAsync(string attrName, string newValue) =>
            await _Session.RequestAsync(DOM.SetAttributeValue(_NodeInfo.Id, attrName, newValue));

        public async Task SetValueAsync(string value)
        {
            await _Session.RequestAsync(DOM.SetNodeValue(_NodeInfo.Id, value));
            _NodeInfo.Value = value;
        }
    }
}
