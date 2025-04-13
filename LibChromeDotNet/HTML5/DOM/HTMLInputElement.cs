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
            string initialValue = (await valueGetter.GetValueAsync()).ToString()!;
            var result = new HTMLInputElement();
            result._Node = node;
            result._Value = initialValue;
            result._ChangeEventListener = await node.AddEventListenerAsync(KeyboardEvent.KeyDown, e => _ = result.OnValueChangedAsync(e));
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

        private IDOMNode _Node;
        private IAsyncDisposable _ChangeEventListener;
        private string _Value;

        public async Task SetValueAsync(string value)
        {
            await using (var jsNode = await _Node.GetJavascriptNodeAsync())
                await jsNode.CallFunctionAsync("HTMLInputElement.prototype.value", IJSValue.FromString(value));
        }

        private async Task OnValueChangedAsync(KeyboardEventArgs e)
        {
            await using (var jsNode = await _Node.GetJavascriptNodeAsync())
            {
                var oldValue = _Value;
                var newValue = (await jsNode.CallFunctionAsync("HTMLInputElement.prototype.value")).ToString();
                if (oldValue != newValue)
                {
                    Interlocked.Exchange(ref _Value, newValue);
                    ValueChanged?.Invoke();
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _ChangeEventListener.DisposeAsync();
            await _ValueGetter.DisposeAsync();
            await _ValueSetter.DisposeAsync();
        }
    }
}
