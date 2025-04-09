using LibChromeDotNet.ChromeInterop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.DOM
{
    public static class DOMExtensions
    {
        public static async Task<IAsyncDisposable> AddEventListenerAsync<TParams>(this IDOMNode node, IDOMEvent<TParams> eventType, Action<TParams> callback)
        {
            var session = node.Session;
            var jsBindingName = Identifier.New();
            await session.AddJSBindingAsync(jsBindingName, (bindingArg) =>
            {
                var eventArgJson = JObject.Parse(bindingArg);
                var eventArg = eventType.GetParamsFromJson(eventArgJson);
                callback(eventArg);
            });
            var serializableLiteralProps = eventType.SerializedProperties
                .Select(propName => $"{propName}: e.{propName}");
            var serializableEventExpr = $"{{{string.Join(',', serializableLiteralProps)}}}";
            var jsHandlerExpr = $"(function(e){{{jsBindingName}(JSON.stringify({serializableEventExpr}));}})";
            var jsHandler = (IJSObject)await session.EvaluateExpressionAsync(jsHandlerExpr);
            var jsNode = await node.GetJavascriptNodeAsync();
            await jsNode.CallFunctionAsync(
                "addEventListener",
                IJSValue.FromString(eventType.EventTypeName),
                jsHandler);
            return new EventListener(jsNode, jsHandler, eventType.EventTypeName);
        }

        public static async Task<IAsyncDisposable> AddEventListenerAsync(this IDOMNode node, GenericDOMEvent eventType, Action callback)
        {
            var session = node.Session;
            var jsBindingName = Identifier.New();
            await session.AddJSBindingAsync(jsBindingName, (bindingArg) => callback());
            var jsHandlerExpr = $"(function(e){{{jsBindingName}(JSON.stringify(e));}})";
            var jsHandler = (IJSObject)await session.EvaluateExpressionAsync(jsHandlerExpr);
            var jsNode = await node.GetJavascriptNodeAsync();
            var eventTypeName = eventType.ToString().ToLowerInvariant();
            await jsNode.CallFunctionAsync(
                "addEventListener",
                IJSValue.FromString(eventTypeName),
                jsHandler);
            return new EventListener(jsNode, jsHandler, eventTypeName);
        }

        public static async Task<string> GetInnerTextAsync(this IDOMNode node)
        {
            var childTextNodes = (await node.GetChildrenAsync())
                .Where(n => n.NodeType == CDP.Domains.DOMNodeType.Text);
            return string.Concat(childTextNodes);
        }

        public static async Task SetInnerTextAsync(this IDOMNode node, string textValue)
        {
            var childTextNodes = (await node.GetChildrenAsync())
                .Where(n => n.NodeType == CDP.Domains.DOMNodeType.Text);
            if (!childTextNodes.Any())
                throw new ArgumentException($"{nameof(node)} has no text children");
            bool firstNode = true;
            foreach (var textChild in childTextNodes)
            {
                if (firstNode)
                {
                    firstNode = false;
                    await textChild.SetValueAsync(textValue);
                }
                else await textChild.DeleteNodeAsync();
            }
        }

        public static async Task<TElement> QuerySelectAsync<TElement>(this IDOMNode node, string selector) where TElement : IHTMLElement
        {
            var queryNode = await node.QuerySelectAsync(selector);
            return await HTMLElement.FromNodeAsync<TElement>(queryNode);
        }

        public static async Task<TElement[]> QuerySelectManyAsync<TElement>(this IDOMNode node, string selector) where TElement : IHTMLElement
        {
            var queryNodes = await node.QuerySelectManyAsync(selector);
            var resultArr = new TElement[queryNodes.Length];
            for (int i = 0; i < resultArr.Length; i++)
                resultArr[i] = await HTMLElement.FromNodeAsync<TElement>(queryNodes[i]);
            return resultArr;
        }

        private class EventListener : IAsyncDisposable
        {
            private IJSObject _JSNode;
            private IJSObject _JSHandler;
            private string _EventTypeName;

            public EventListener(IJSObject jSNode, IJSObject jSHandler, string eventTypeName)
            {
                _JSNode = jSNode;
                _JSHandler = jSHandler;
                _EventTypeName = eventTypeName;
            }

            public async ValueTask DisposeAsync()
            {
                await _JSNode.CallFunctionAsync(
                    "removeEventListener",
                    IJSValue.FromString(_EventTypeName),
                    _JSHandler);
                await _JSNode.DisposeAsync();
                await _JSHandler.DisposeAsync();
            }
        }
    }
}
