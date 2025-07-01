using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5.JS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.CSS
{
    public static class CSSExtensions
    {
        public static async Task<ICSSClassList> GetClassListAsync(this IDOMNode node)
        {
            await using (var jsNode = await node.GetJavascriptNodeAsync())
            await using (var binding = await jsNode.BindAsync())
            {
                var jsClassList = (IJSObject)await binding.GetAsync("classList");
                return new ClassList(jsClassList);
            }
        }

        private class ClassList : ICSSClassList
        {
            public ClassList(IJSObject jsClassList)
            {
                _JSClassList = jsClassList;
            }

            private IJSObject _JSClassList;

            public async Task<bool> ContainsAsync(string className)
            {
                var result = await _JSClassList.CallFunctionAsync("DOMTokenList.prototype.contains", IJSValue.FromString(className));
                return ((IJSValue<bool>)result).Value;
            }

            public async Task<bool> ToggleAsync(string className)
            {
                var result = await _JSClassList.CallFunctionAsync("DOMTokenList.prototype.toggle", IJSValue.FromString(className));
                return ((IJSValue<bool>)result).Value;
            }

            public async Task AddAsync(string className)
            {
                await _JSClassList.CallFunctionAsync("DOMTokenList.prototype.add", IJSValue.FromString(className));
            }

            public async Task RemoveAsync(string className)
            {
                await _JSClassList.CallFunctionAsync("DOMTokenList.prototype.remove", IJSValue.FromString(className));
            }

            public async ValueTask DisposeAsync()
            {
                await _JSClassList.DisposeAsync();
            }
        }
    }
}
