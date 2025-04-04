using LibChromeDotNet.WebInterop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP.Domains
{
    public static class Runtime
    {
        public static ICDPRequest AddBinding(string name, string? executionContextName = null)
        {
            var jsonParams = new JObject();
            jsonParams.Add("name", name);
            if (executionContextName != null)
                jsonParams.Add("executionContextName", executionContextName);
            return CDP.Request("Runtime.addBinding", jsonParams);
        }

        public static ICDPRequest<RemoteObject> CallFunctionOn(string objectId, string functionName, params RemoteObject[] arguments)
        {
            var jsonParams = new JObject()
            {
                { "functionDeclaration", functionName },
                { "objectId", objectId },
                { "arguments", CreateArgumentsJson(arguments) }
            };
            return CDP.Request("Runtime.callFunctionOn", jsonParams, resultJson => new RemoteObject(resultJson));
        }

        public static ICDPRequest<RemoteObject> CallFunctionOn(int executionContextId, string functionName, params RemoteObject[] arguments)
        {
            var jsonParams = new JObject()
            {
                { "functionDeclaration", functionName },
                { "objectId", executionContextId },
                { "arguments", CreateArgumentsJson(arguments) }
            };
            return CDP.Request("Runtime.callFunctionOn", jsonParams, resultJson => new RemoteObject(resultJson));
        }

        private static JArray CreateArgumentsJson(RemoteObject[] arguments)
        {
            var jsonArray = new JArray();
            foreach (var obj in arguments)
            {
                var callArgumentJson = new JObject();
                if (obj.ObjectId != null)
                    callArgumentJson.Add("objectId", obj.ObjectId);
                else if (obj.Value != null)
                    callArgumentJson.Add("value", obj.Value);
            }
            return jsonArray;
        }
    }

    public struct RemoteObject
    {
        public JSType Type;
        public JValue? Value;
        public string? ObjectId;

        public RemoteObject(JObject json)
        {
            Type = Enum.Parse<JSType>(json["type"]!.ToString(), true);
            var valJson = json["value"];
            var objectIdJson = json["objectId"];
            if (valJson != null)
                Value = (JValue)valJson;
            if (objectIdJson != null)
                ObjectId = objectIdJson.ToString();
        }
    }
}
