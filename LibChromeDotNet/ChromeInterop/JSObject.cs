using LibChromeDotNet.CDP.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public class JSObject : IJSObject
    {
        private IInteropSession _Session;
        protected string _ObjectId;

        public JSObject(IInteropSession session, string objectId)
        {
            _Session = session;
            _ObjectId = objectId;
        }

        public JSType Type => JSType.Object;
        public IInteropSession Session => _Session;

        public virtual RemoteObject AsRemoteObject()
        {
            return new RemoteObject()
            {
                Type = JSType.Object,
                ObjectId = _ObjectId
            };
        }

        public async Task<IJSValue> CallFunctionAsync(string name, params IJSValue[] arguments)
        {
            var remoteObject = await _Session.RequestAsync(Runtime.CallFunctionOn(_ObjectId, name, arguments.Select(a => a.AsRemoteObject())));
            return IJSValue.FromRemoteObject(_Session, remoteObject);
        }
    }
}
