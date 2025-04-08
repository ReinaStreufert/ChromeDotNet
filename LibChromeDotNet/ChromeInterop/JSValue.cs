using LibChromeDotNet.CDP.Domains;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public class JSValue<T> : IJSValue<T>
    {
        private IInteropSession _Session;
        private JValue _SerializedValue;
        private JSType _Type;

        public JSValue(IInteropSession session, JValue serializedValue, JSType type)
        {
            _SerializedValue = serializedValue;
            _Session = session;
            _Type = type;
        }

        public T Value => _SerializedValue.Value<T>()!;
        public JSType Type { get; }
        public IInteropSession Session => _Session;

        public RemoteObject AsRemoteObject()
        {
            return new RemoteObject()
            {
                ObjectId = null,
                Type = _Type,
                Value = _SerializedValue
            };
        }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    public class JSValue : IJSValue
    {
        private IInteropSession _Session;
        private RemoteObject _RemoteObject;

        public JSValue(IInteropSession session, RemoteObject remoteObject)
        {
            _Session = session;
            _RemoteObject = remoteObject;
        }

        public JSType Type => _RemoteObject.Type;
        public IInteropSession Session => _Session;

        public RemoteObject AsRemoteObject() => _RemoteObject;
    }
}
