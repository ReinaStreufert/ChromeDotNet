using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP
{
    public interface ICDPResource
    {
        string MethodName { get; }
    }

    // for message types that dont get a response
    public interface ICDPRequest : ICDPResource
    {
        JObject GetJsonParams();
    }

    // for message types that get a response (converted to type "T")
    public interface ICDPRequest<TResult> : ICDPRequest
    {
        public TResult GetResultFromJson(JObject resultObject);
    }

    public interface ICDPEvent<TParams> : ICDPResource
    {
        public void Handle(JObject jsonParams, Action<TParams> callback);
    }
}
