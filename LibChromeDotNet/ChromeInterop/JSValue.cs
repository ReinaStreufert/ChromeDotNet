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
        private JValue _SerializedValue;
        private JSType _Type;

        public JSValue(JValue serializedValue, JSType type)
        {
            _SerializedValue = serializedValue;
            _Type = type;
        }

        public T Value => _SerializedValue.Value<T>()!;
        public JSType Type { get; }

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
        private RemoteObject _RemoteObject;

        public JSValue(RemoteObject remoteObject)
        {
            _RemoteObject = remoteObject;
        }

        public JSType Type => _RemoteObject.Type;

        public RemoteObject AsRemoteObject() => _RemoteObject;
    }
}
