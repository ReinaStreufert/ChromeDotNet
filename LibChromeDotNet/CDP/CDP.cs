using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP
{
    public static class CDP
    {
        public static ICDPRequest Request(string method, JObject jsonParams) =>
            new CDPRequest(method, jsonParams);
        public static ICDPRequest Request(string method) => Request(method, new JObject());
        public static ICDPRequest<TResult> Request<TResult>(string method, JObject jsonParams, Func<JObject, TResult> resultParseCallback) => 
            new CDPRequest<TResult>(method, jsonParams, resultParseCallback);
        public static ICDPEvent<TParams> Event<TParams>(string eventName, Func<JObject, TParams> paramsParseCallback) =>
            new CDPEvent<TParams>(eventName, paramsParseCallback);
    }
}
