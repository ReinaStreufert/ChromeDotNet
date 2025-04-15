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
        private object _Sync = new object();

        public async Task SetTextAsync(string text)
        {
            for (; ;)
            {
                var oldNode = _Node;
                var newNode = await _Node.SetInnerTextAsync(text);
                lock (_Sync)
                {
                    if (_Node == oldNode)
                    {
                        _Text = text;
                        _Node = newNode;
                        break;
                    }
                    // in the case of concurrent calls to SetTextAsync, a previous call may replace _Node while this call awaits
                    // the response from oldNode. this one mean the second call would likely fail and be ignored. i decided to handle this case
                    // and restart the process if _Node was replaced by checking after acquiring the lock...
                }
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
