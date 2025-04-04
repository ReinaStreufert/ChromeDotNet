using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public interface IJSObject : IJSContext
    {
        public JSType Type { get; }
        public Task<IEnumerable<IJSProperty>> GetPropertiesAsync();
        public Task<IEnumerable<string>> GetKeysAsync();
        public Task<IEnumerable<IJSObject>> GetValuesAsync();
    }

    public interface IJSFunction : IJSObject
    {
        public Task<IJSObject?> CallAsync(params IJSObject[] arguments);
        public Task<IJSObject?> CallAsync(IJSExecutionContext executionContext, params IJSObject[] arguments);
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
