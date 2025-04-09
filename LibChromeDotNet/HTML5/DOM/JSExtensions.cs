using LibChromeDotNet.ChromeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.DOM
{
    // optimize JS property bindings without writing script modules which currently dont work for some reason.
    public static class JSExtensions
    {
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
