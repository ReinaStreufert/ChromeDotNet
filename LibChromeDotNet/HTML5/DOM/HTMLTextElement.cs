using LibChromeDotNet.ChromeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.DOM
{
    // this class wraps multiple HTML elements that have the purpose of displaying text
    // eventually im gonna fill this class w/ APIs for programmatically changing rich text
    public class HTMLTextElement : IHTMLElement
    {
        public static async Task<HTMLTextElement> FromDOMNodeAsync(IDOMNode node)
        {
            var result = new HTMLTextElement();
            result._Text = await node.GetInnerTextAsync();
            result._Node = node;
            return result;
        }

        public IDOMNode Node => _Node;
        public string Text { get => _Text; set => _ = SetTextAsync(value); }

        private HTMLTextElement() { }

        private IDOMNode _Node;
        private string _Text;

        public async Task SetTextAsync(string text)
        {
            await _Node.SetInnerTextAsync(text);
            Interlocked.Exchange(ref _Text, text);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
