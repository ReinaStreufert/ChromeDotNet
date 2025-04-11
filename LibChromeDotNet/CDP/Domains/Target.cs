using LibChromeDotNet.ChromeInterop;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP.Domains
{
    public static class Target
    {
        public static ICDPRequest<IEnumerable<TargetInfo>> GetTargets(string? filter = null)
        {
            var paramsJson = new JObject();
            if (filter != null)
                paramsJson.Add("filter", filter);
            return CDP.Request("Target.getTargets", paramsJson, jsonResult =>
            {
                return jsonResult["targetInfos"]!
                    .Cast<JObject>()
                    .Select(o => new TargetInfo(o));
            });
        }

        public static ICDPRequest<string> CreateTarget(Uri url, string? browserContextId = null, bool newWindow = true, int width = 0, int height = 0)
        {
            var paramsJson = new JObject()
            {
                { "url", url.ToString() },
                { "newWindow", newWindow }
            };
            if (browserContextId != null)
                paramsJson.Add("browserContextId", browserContextId);
            if (width > 0 && height > 0)
            {
                paramsJson.Add("width", width);
                paramsJson.Add("height", height);
            }
            return CDP.Request("Target.createTarget", paramsJson, jsonResult => jsonResult["targetId"]!.ToString());
        }

        public static ICDPRequest<string> AttachToTarget(string targetId, bool flatten = true)
        {
            var paramsJson = new JObject
            {
                { "targetId", targetId },
                { "flatten", flatten }
            };
            return CDP.Request("Target.attachToTarget", paramsJson, jsonResult =>
            {
                return jsonResult["sessionId"]!.ToString();
            });
        }

        public static ICDPRequest DetachFromTarget(string sessionId)
        {
            var paramsJson = new JObject
            {
                { "sessionId", sessionId }
            };
            return CDP.Request("Target.detachFromTarget");
        }

        public static ICDPRequest SetDiscoverTargets(bool enabled)
        {
            var paramsJson = new JObject
            {
                { "discover", enabled }
            };
            return CDP.Request("Target.setDiscoverTargets", paramsJson);
        }

        public static ICDPEvent<TargetInfo> OnTargetCreated => CDP.Event("Target.targetCreated", resultJson => new TargetInfo((JObject)resultJson["targetInfo"]!));
        public static ICDPEvent<string> OnTargetDestroyed => CDP.Event("Target.targetDestroyed", resultJson => resultJson["targetId"]!.ToString());
    }

    public struct TargetInfo : IInteropTarget
    {
        public string Id;
        public string BrowserContextId;
        public DebugTargetType Type;
        public string Title;
        public Uri NavigationUri;

        public TargetInfo(JObject jsonObject)
        {
            Id = jsonObject["targetId"]!.ToString();
            BrowserContextId = jsonObject["browserContextId"]!.ToString();
            Type = Enum.Parse<DebugTargetType>(jsonObject["type"]!.ToString(), true);
            Title = jsonObject["title"]!.ToString();
            NavigationUri = new Uri(jsonObject["url"]!.ToString());
        }

        string IInteropTarget.Id => Id;
        DebugTargetType IInteropTarget.Type => Type;
        string IInteropTarget.Title => Title;
        Uri IInteropTarget.NavigationUri => NavigationUri;
        string IInteropTarget.BrowserContextId => BrowserContextId;
    }
}
