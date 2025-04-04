using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public interface IJSContext
    {
        public Task<IJSObject?> CallFunctionAsync(string name, params IJSObject[] arguments);
    }

    public interface IJSExecutionContext : IJSContext
    {
        public string Name { get; }
        public Task<IJSObject> GetGlobalAsync();
        public Task<IJSObject?> EvaluateAsync(string scriptText);
        public Task ExposeFunctionAsync(string functionName, Action<string> bindingCallback);
    }
}
