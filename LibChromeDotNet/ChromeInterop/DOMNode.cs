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

        public string Name => throw new NotImplementedException();

        public Task DeleteNodeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetAttributesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IDOMNode>> GetChildrenAsync()
        {
            throw new NotImplementedException();
        }

        public IJSObject GetJavascriptNodeAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IDOMNode> QuerySelectAsync(string selector)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IDOMNode>> QuerySelectManyAsync(string selector)
        {
            throw new NotImplementedException();
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
