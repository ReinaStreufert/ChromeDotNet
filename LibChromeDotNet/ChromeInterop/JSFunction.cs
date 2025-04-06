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

        public Task<IJSValue> CallAsync(params IJSObject[] arguments)
        {
            throw new NotImplementedException();
        }
    }
}
