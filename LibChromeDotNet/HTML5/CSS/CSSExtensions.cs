using LibChromeDotNet.ChromeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.CSS
{
    public static class CSSExtensions
    {
        private class ClassList : ICSSClassList
        {
            public ClassList(IJSObject classListJS)
            {
                _ClassListJS = classListJS;
            }

            private IJSObject _ClassListJS;

            public async Task<bool> ContainsAsync(string className)
            {
                var result = await _ClassListJS.CallFunctionAsync("DOMTokenList.prototype.contains", IJSValue.FromString(className));
                return ((IJSValue<bool>)result).Value;
            }

            public async Task<bool> ToggleAsync(string className)
            {
                var result = await _ClassListJS.CallFunctionAsync("DOMTokenList.prototype.toggle", IJSValue.FromString(className));
                return ((IJSValue<bool>)result).Value;
            }

            public async Task AddAsync(string className)
            {
                await _ClassListJS.CallFunctionAsync("DOMTokenList.prototype.add", IJSValue.FromString(className));
            }

            public async Task RemoveAsync(string className)
            {
                await _ClassListJS.CallFunctionAsync("DOMTokenList.prototype.remove", IJSValue.FromString(className));
            }

            public async ValueTask DisposeAsync()
            {
                await _ClassListJS.DisposeAsync();
            }
        }
    }
}
