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
            IJSGetter valueGetter;
            IJSSetter valueSetter;
            await using (var jsNode = await node.GetJavascriptNodeAsync())
            {
                valueGetter = await jsNode.BindGetterAsync("value");
                valueSetter = await jsNode.BindSetterAsync("value");
            }
            string initialValue = (await valueGetter.GetValueAsync()).ToString()!;
            var result = new HTMLInputElement();
            result._Node = node;
            result._ValueGetter = valueGetter;
            result._ValueSetter = valueSetter;
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
        private IJSGetter _ValueGetter;
        private IJSSetter _ValueSetter;
        private string _Value;

        public async Task SetValueAsync(string value) => await _ValueSetter.SetValueAsync(IJSValue.FromString(value));

        private async Task OnValueChangedAsync(KeyboardEventArgs e)
        {
            await using (var jsNode = await _Node.GetJavascriptNodeAsync())
            {
                var oldValue = _Value;
                var newValue = (await _ValueGetter.GetValueAsync()).ToString()!;
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
