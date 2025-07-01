using LibChromeDotNet.ChromeInterop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5
{
    public interface IAppWindow
    {
        public event Action? ClosedByUser;
        public Task CloseAsync();
        public Task<IDOMNode> GetDocumentBodyAsync();
        public Task NavigateAsync(string contentPath);
        public Task<IJSValue> EvaluateJSExpressionAsync(string expr);
        public Task<AwaitableJSBinding> AddJSAwaitableBindingAsync();
        public Task<IJSFunction> AddJSBindingAsync(Action callback);
        public Task<IJSFunction> AddJSBindingAsync(Action<string> callback);
        public Task<IJSFunction> AddJSBindingAsync(Action<JObject> callback);
        public string DocumentLocation { get; }
    }

    public struct AwaitableJSBinding
    {
        public Task Task { get; init; }
        public IJSFunction CompletionSignal { get; init; }
    }
}
