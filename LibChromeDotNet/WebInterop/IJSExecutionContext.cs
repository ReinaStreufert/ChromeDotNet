using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.WebInterop
{
    public interface IJSEnvironment
    {
        public Task<IJSObject?> CallFunctionAsync(string name, params IJSObject[] arguments);
    }

    public interface IJSExecutionContext : IJSEnvironment
    {
        public string Name { get; }
        public IJSObject Global { get; }
        public Task<IJSObject?> EvaluateAsync(string scriptText);
        public Task ExposeFunctionAsync(string functionName, Action<string> bindingCallback);
    }
}
