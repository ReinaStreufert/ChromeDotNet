using LibChromeDotNet.CDP.Domains;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public interface IJSValue
    {
        public JSType Type { get; }
        public RemoteObject AsRemoteObject();

        public static IJSValue Undefined => new JSValue(new RemoteObject()
        {
            Type = JSType.Undefined
        });

        public static IJSValue FromRemoteObject(IInteropSession session, RemoteObject remoteObject)
        {
            return remoteObject.Type switch
            {
                JSType.String => new JSValue<string>(remoteObject.Value!, JSType.String),
                JSType.Boolean => new JSValue<bool>(remoteObject.Value!, JSType.Boolean),
                JSType.Number => new JSValue<double>(remoteObject.Value!, JSType.Number),
                JSType.Object => new JSObject(session, remoteObject.ObjectId!),
                JSType.Function => new JSFunction(session, remoteObject.ObjectId!),
                _ => new JSValue(remoteObject)
            };
        }

        public static IJSValue FromString(string text)
        {
            return new JSValue(new RemoteObject()
            {
                Type = JSType.String,
                Value = new JValue(text)
            });
        }

        public static IJSValue FromNumber(double value)
        {
            return new JSValue(new RemoteObject()
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

    public interface IJSObject : IJSValue, IInteropObject, IAsyncDisposable
    {
        public Task<IJSValue> CallFunctionAsync(string name, params IJSValue[] arguments);
    }

    public interface IJSFunction : IJSObject
    {
        public Task<IJSValue> CallAsync(params IJSValue[] arguments);
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
