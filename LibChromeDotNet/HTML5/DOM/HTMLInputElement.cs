using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5.JS;
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
            IJSObjectBinding binding;
            await using (var jsNode = await node.GetJavascriptNodeAsync())
                binding = await jsNode.BindAsync();
            string initialValue = (await binding.GetAsync("value")).ToString()!;
            var result = new HTMLInputElement();
            result._Node = node;
            result._Binding = binding;
            result._Value = initialValue;
            result._ChangeEventListener = await node.AddEventListenerAsync(Event.KeyDown, e => _ = result.OnValueChangedAsync(e));
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
        private IJSObjectBinding _Binding;
        private string _Value;

        public async Task SetValueAsync(string value) => await _Binding.SetAsync("value", IJSValue.FromString(value));
        private async Task OnValueChangedAsync(KeyboardEventArgs e)
        {
            await using (var jsNode = await _Node.GetJavascriptNodeAsync())
            {
                var oldValue = _Value;
                var newValue = (await _Binding.GetAsync("value")).ToString()!;
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
            await _Binding.DisposeAsync();
        }
    }
}