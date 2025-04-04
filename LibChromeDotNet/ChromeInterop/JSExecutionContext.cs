using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public class JSExecutionContext : IJSExecutionContext
    {
        public string Name { get; }

        public JSExecutionContext(int id, string name)
        {

        }

        private InteropSession _Session;
        private int _ExecutionContextId;
        private string _IsolatedWorldName;

        public Task<IJSObject?> CallFunctionAsync(string name, params IJSObject[] arguments)
        {
            throw new NotImplementedException();
        }

        public Task<IJSObject?> EvaluateAsync(string scriptText)
        {
            throw new NotImplementedException();
        }

        public Task ExposeFunctionAsync(string functionName, Action<string> bindingCallback)
        {
            throw new NotImplementedException();
        }

        public Task<IJSObject> GetGlobalAsync()
        {
            throw new NotImplementedException();
        }
    }
}
