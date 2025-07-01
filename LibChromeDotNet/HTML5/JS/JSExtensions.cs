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

        // also these, im dumb asf
        public static async Task<IJSGetter> BindGetterAsync(this IJSObject obj, string propIndex)
        {
            var session = obj.Session;
            var getterFactoryExpr = "(function(obj, propIndex){ return (function(){ return obj[propIndex]; }); })";
            await using (var getterFactory = (IJSFunction)await session.EvaluateExpressionAsync(getterFactoryExpr))
            {
                var getterFunc = await getterFactory.CallAsync(obj, IJSValue.FromString(propIndex));
                return new JSGetter((IJSFunction)getterFunc);
            }
        }

        public static async Task<IJSSetter> BindSetterAsync(this IJSObject obj, string propIndex)
        {
            var session = obj.Session;
            var setterFactoryExpr = "(function(obj, propIndex){ return (function(value){ obj[propIndex] = value; }); })";
            await using (var setterFactory = (IJSFunction)await session.EvaluateExpressionAsync(setterFactoryExpr))
            {
                var setterFunc = await setterFactory.CallAsync(obj, IJSValue.FromString(propIndex));
                return new JSSetter((IJSFunction)setterFunc);
            }
        }

        private class JSGetter : IJSGetter
        {
            private IJSFunction _GetterFunc;

            public JSGetter(IJSFunction getterFunc)
            {
                _GetterFunc = getterFunc;
            }

            public ValueTask DisposeAsync() => _GetterFunc.DisposeAsync();
            public async Task<IJSValue> GetValueAsync() => await _GetterFunc.CallAsync();
        }

        private class JSSetter : IJSSetter
        {
            private IJSFunction _SetterFunc;

            public JSSetter(IJSFunction setterFunc)
            {
                _SetterFunc = setterFunc;
            }

            public ValueTask DisposeAsync() => _SetterFunc.DisposeAsync();
            public async Task SetValueAsync(IJSValue value) => await _SetterFunc.CallAsync(value);
        }
    }

    public interface IJSGetter : IAsyncDisposable
    {
        Task<IJSValue> GetValueAsync();
    }

    public interface IJSSetter : IAsyncDisposable
    {
        Task SetValueAsync(IJSValue value);
    }
}
