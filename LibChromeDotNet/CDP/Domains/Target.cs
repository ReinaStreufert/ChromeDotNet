using LibChromeDotNet.WebInterop;
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
    }

    public struct TargetInfo : IInteropTarget
    {
        public string Id;
        public DebugTargetType Type;
        public string Title;
        public Uri NavigationUri;

        public TargetInfo(JObject jsonObject)
        {
            Id = jsonObject["targetId"]!.ToString();
            Type = Enum.Parse<DebugTargetType>(jsonObject["type"]!.ToString(), true);
            Title = jsonObject["title"]!.ToString();
            NavigationUri = new Uri(jsonObject["url"]!.ToString());
        }

        string IInteropTarget.Id => Id;
        DebugTargetType IInteropTarget.Type => Type;
        string IInteropTarget.Title => Title;
        Uri IInteropTarget.NavigationUri => NavigationUri;
    }
}
