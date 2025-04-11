using LibChromeDotNet.ChromeInterop;
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
            var bindingTempName = Identifier.New();
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
    }
}
