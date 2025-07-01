using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5.DOM;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.JS
{
    public static class JSExtensions
    {
        // i wish the cdp api just gave you an anonymous binding function and you could put it in a global var
        // if you wanted to...instead i will do an obvious workaround which shows how obviously easy it wouldve been for them to
        // just implement it like this in the first place which would be you know good.
        public static async Task<IJSFunction> AddJSBindingAsync(this IInteropSession session, Action<string> callback)
        {
            var bindingTempName = JSIdentifier.New();
            await session.AddJSBindingAsync(bindingTempName, callback);
            var result = (IJSFunction)await session.EvaluateExpressionAsync(bindingTempName);
            await session.EvaluateExpressionAsync($"delete {bindingTempName};");
            return result;
            // the fact it has a name in the global scope for any amount of time even though its less than like a microsecond still pisses me off just in principal. why
        }

        public static async Task<IJSFunction> AddJSBindingAsync(this IInteropSession session, Action<JObject> jsonCallback) // smirk emoji
        {
            var strJsBinding = await session.AddJSBindingAsync((string s) => jsonCallback(JObject.Parse(s)));
            const string jsBindingFactoryExpr = "(function(strBinding){ return (function(p){ strBinding(JSON.stringify(p)); }) })";
            await using (var jsBindingFactory = (IJSFunction)await session.EvaluateExpressionAsync(jsBindingFactoryExpr))
                return (IJSFunction)await jsBindingFactory.CallAsync(strJsBinding);
        }

        public static async Task<IJSFunction> AddJSBindingAsync(this IInteropSession session, Action callback)
        {
            var strJsBinding = await session.AddJSBindingAsync((string s) => callback());
            const string jsBindingFactoryExpr = "(function(strBinding){ return function(){ stringBinding(\"\"); } })";
            await using (var jsBindingFactory = (IJSFunction)await session.EvaluateExpressionAsync(jsBindingFactoryExpr))
                return (IJSFunction)await jsBindingFactory.CallAsync(strJsBinding);
        }

        public static async Task<IJSObjectBinding> BindAsync(this IJSObject obj)
        {
            var session = obj.Session;
            var getterFactoryExpr = "(function(obj){ return (function(propIndex){ return obj[propIndex]; }); })";
            var setterFactoryExpr = "(function(obj){ return (function(propIndex, value){ obj[propIndex] = value; }); })";
            await using (var getterFactory = (IJSFunction)await session.EvaluateExpressionAsync(getterFactoryExpr))
            await using (var setterFactory = (IJSFunction)await session.EvaluateExpressionAsync(setterFactoryExpr))
            {
                var getterFunc = (IJSFunction)await getterFactory.CallAsync(obj);
                var setterFunc = (IJSFunction)await setterFactory.CallAsync(obj);
                return new JSObjectBinding(getterFunc, setterFunc);
            }
        }

        private class JSObjectBinding : IJSObjectBinding
        {
            private IJSFunction _GetterFunc;
            private IJSFunction _SetterFunc;

            public JSObjectBinding(IJSFunction getterFunc, IJSFunction setterFunc)
            {
                _GetterFunc = getterFunc;
                _SetterFunc = setterFunc;
            }

            public async Task<IJSValue> CallPropertyAsync(string key, params IJSValue[] args)
            {
                var callTarget = (IJSFunction)await _GetterFunc.CallAsync(IJSValue.FromString(key));
                return await callTarget.CallAsync(args);
            }

            public async Task<IJSValue> GetAsync(string key)
            {
                return await _GetterFunc.CallAsync(IJSValue.FromString(key));
            }

            public async Task SetAsync(string key, IJSValue value)
            {
                await _SetterFunc.CallAsync(IJSValue.FromString(key), value);
            }

            public async ValueTask DisposeAsync()
            {
                await _GetterFunc.DisposeAsync();
                await _SetterFunc.DisposeAsync();
            }
        }
    }

    public interface IJSObjectBinding : IAsyncDisposable
    {
        public Task<IJSValue> GetAsync(string key);
        public Task SetAsync(string key, IJSValue value);
        public Task<IJSValue> CallPropertyAsync(string key, params IJSValue[] args); // javascript is silly
    }
}
