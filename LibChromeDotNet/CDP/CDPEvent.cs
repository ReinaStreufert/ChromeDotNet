using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP
{
    public class CDPEvent<TParams> : ICDPEvent<TParams>
    {
        public string MethodName { get; }

        public CDPEvent(string methodName, Func<JObject, TParams> paramParseCallback)
        {
            MethodName = methodName;
            _paramParseCallback = paramParseCallback;
        }

        private Func<JObject, TParams> _paramParseCallback;

        public void Handle(JObject jsonParams, Action<TParams> callback)
        {
            callback(_paramParseCallback(jsonParams));
        }
    }
}
