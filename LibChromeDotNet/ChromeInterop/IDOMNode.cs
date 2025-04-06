using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public interface IDOMNode : IInteropObject
    {
        public string Name { get; }
        public Task<IJSObject> GetJavascriptNodeAsync();
        public Task<IEnumerable<KeyValuePair<string, string>>> GetAttributesAsync();
        public Task SetAttributeAsync(string attrName, string newValue);
        public Task RemoveAttributeAsync(string attrName);
        public Task SetValueAsync(string value);
        public Task<IDOMNode> QuerySelectAsync(string selector);
        public Task<IEnumerable<IDOMNode>> GetChildrenAsync();
        public Task<IDOMNode[]> QuerySelectManyAsync(string selector);
        public Task DeleteNodeAsync();
    }
}
