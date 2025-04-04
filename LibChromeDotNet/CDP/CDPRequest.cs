using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP
{
    public class CDPRequest : ICDPRequest
    {
        public string MethodName { get; }

        public CDPRequest(string methodName, JObject jsonParams)
        {
            MethodName = methodName;
            _JsonParams = jsonParams;
        }

        private JObject _JsonParams { get; }

        public JObject GetJsonParams()
        {
            return _JsonParams;
        }
    }

    public class CDPRequest<TResult> : ICDPRequest<TResult>
    {
        public string MethodName { get; }

        public CDPRequest(string methodName, JObject jsonParams, Func<JObject, TResult> resultParseCallback)
        {
            MethodName = methodName;
            _JsonParams = jsonParams;
            _ResultParseCallback = resultParseCallback;
        }

        private JObject _JsonParams;
        private Func<JObject, TResult> _ResultParseCallback;

        public JObject GetJsonParams() => _JsonParams;
        public TResult GetResultFromJson(JObject resultObject) => _ResultParseCallback(resultObject);
    }
}
