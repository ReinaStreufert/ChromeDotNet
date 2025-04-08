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
        public static async Task AddEventListenerAsync<TParams>(this IDOMNode node, IDOMEvent<TParams> eventType, Action<TParams> callback)
        {
            var session = node.Session;
            var jsBindingName = Identifier.New();
            await session.AddJSBindingAsync(jsBindingName, (bindingArg) =>
            {
                var eventArgJson = JObject.Parse(bindingArg);
                var eventArg = eventType.GetParamsFromJson(eventArgJson);
                callback(eventArg);
            });
            var jsHandlerExpr = $"(function(e){{{jsBindingName}(JSON.stringify(e));}})";
            using (var jsHandler = (IJSObject)await session.EvaluateExpressionAsync(jsHandlerExpr))
            using (var jsNode = await node.GetJavascriptNodeAsync())
            {
                await jsNode.CallFunctionAsync(
                    "addEventListener",
                    IJSValue.FromString(session, eventType.EventTypeName),
                    jsHandler);
            }
        }

        public static async Task AddEventListenerAsync(this IDOMNode node, GenericDOMEvent eventType, Action callback)
        {
            var session = node.Session;
            var jsBindingName = Identifier.New();
            await session.AddJSBindingAsync(jsBindingName, (bindingArg) => callback());
            var jsHandlerExpr = $"(function(e){{{jsBindingName}(JSON.stringify(e));}})";
            using (var jsHandler = (IJSObject)await session.EvaluateExpressionAsync(jsHandlerExpr))
            using (var jsNode = await node.GetJavascriptNodeAsync())
            {
                await jsNode.CallFunctionAsync(
                    "addEventListener",
                    IJSValue.FromString(session, eventType.ToString().ToLowerInvariant()),
                    jsHandler);
            }
        }
    }
}
