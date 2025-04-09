using LibChromeDotNet.ChromeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.DOM
{
    public class HTMLInputElement : IHTMLElement
    {
        public static async Task<HTMLInputElement> FromDOMNodeAsync(IDOMNode node)
        {
            var session = node.Session;
            var module = await session.RequireModuleAsync(ManifestJSModule.FromScriptName("htmlInputElement.js"));
            string initialValue;
            await using (var jsNode = await node.GetJavascriptNodeAsync())
                initialValue = (await module.CallFunctionAsync("getValue", jsNode)).ToString()!;
            var result = new HTMLInputElement();
            result._Node = node;
            result._InputModule = module;
            result._Value = initialValue;
            result._ChangeEventListener = await node.AddEventListenerAsync(GenericDOMEvent.Change, () => _ = result.OnValueChangedAsync());
            return result;
        }

        public IDOMNode Node => _Node;
        public event Action? ValueChanged;

        public string Value
        {
            get => _Value;
            set => _ = SetValueAsync(value);
        }

        private HTMLInputElement() { }

        private IJSObject _InputModule;
        private IDOMNode _Node;
        private IAsyncDisposable _ChangeEventListener;
        private string _Value;

        public async Task SetValueAsync(string value)
        {
            // _Value is always updated by the event listener
            await using (var jsNode = await _Node.GetJavascriptNodeAsync())
            {
                await _InputModule.CallFunctionAsync(
                    "setValue",
                    jsNode,
                    IJSValue.FromString(_InputModule.Session, value));
            }
        }

        private async Task OnValueChangedAsync()
        {
            await using (var jsNode = await _Node.GetJavascriptNodeAsync())
            {
                var newValue = (await _InputModule.CallFunctionAsync("getValue", jsNode)).ToString();
                Interlocked.Exchange(ref _Value, newValue);
                ValueChanged?.Invoke();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _ChangeEventListener.DisposeAsync();
        }
    }
}
