using LibChromeDotNet.CDP.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public class JSFunction : JSObject, IJSFunction
    {
        public JSFunction(IInteropSession session, string objectId) : base(session, objectId)
        {
        }

        public override RemoteObject AsRemoteObject()
        {
            return new RemoteObject()
            {
                Type = JSType.Function,
                ObjectId = _ObjectId
            };
        }

        public async Task<IJSValue> CallAsync(params IJSValue[] arguments)
        {
            var prependedArgs = new IJSValue[arguments.Length + 1];
            prependedArgs[0] = IJSValue.Undefined;
            for (int i = 0; i < arguments.Length; i++)
                prependedArgs[i + 1] = arguments[i];
            return await CallFunctionAsync("call", prependedArgs);
        }
    }
}
