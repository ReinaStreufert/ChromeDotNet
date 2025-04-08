using LibChromeDotNet.CDP.Domains;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public interface IJSValue : IInteropObject
    {
        public JSType Type { get; }
        public RemoteObject AsRemoteObject();

        public static IJSValue FromRemoteObject(IInteropSession session, RemoteObject remoteObject)
        {
            return remoteObject.Type switch
            {
                JSType.String => new JSValue<string>(session, remoteObject.Value!, JSType.String),
                JSType.Boolean => new JSValue<bool>(session, remoteObject.Value!, JSType.Boolean),
                JSType.Number => new JSValue<double>(session, remoteObject.Value!, JSType.Number),
                JSType.Object => new JSObject(session, remoteObject.ObjectId!),
                JSType.Function => new JSFunction(session, remoteObject.ObjectId!),
                _ => new JSValue(session, remoteObject)
            };
        }

        public static IJSValue FromString(IInteropSession session, string text)
        {
            return new JSValue(session, new RemoteObject()
            {
                Type = JSType.String,
                Value = new JValue(text)
            });
        }

        public static IJSValue FromNumber(IInteropSession session, double value)
        {
            return new JSValue(session, new RemoteObject()
            {
                Type = JSType.Number,
                Value = new JValue(value)
            });
        }
    }

    public interface IJSValue<TVal> : IJSValue
    {
        public TVal Value { get; }
    }

    public interface IJSString : IJSValue<string> { }
    public interface IJSNumber : IJSValue<double> { }
    public interface IJSBoolean : IJSValue<bool> { }

    public interface IJSObject : IJSValue, IDisposable
    {
        public Task<IJSValue> CallFunctionAsync(string name, params IJSValue[] arguments);
    }

    public interface IJSFunction : IJSObject
    {
        public Task<IJSValue> CallAsync(params IJSObject[] arguments);
    }

    public interface IJSProperty
    {
        public string Name { get; }
        public bool Writable { get; }
        public IJSFunction? Getter { get; }
        public IJSFunction? Setter { get; }
        public Task<IJSObject> GetValueAsync();
        public Task SetValueAsync(IJSObject value);
    }

    public enum JSType
    {
        Object,
        Function,
        Undefined,
        String,
        Number,
        Boolean,
        Symbol,
        BigInt
    }
}
