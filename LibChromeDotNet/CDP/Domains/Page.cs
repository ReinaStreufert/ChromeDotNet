using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP.Domains
{
    public static class Page
    {
        public static ICDPRequest Enable => CDP.Request("Page.enable");

        public static ICDPRequest Close => CDP.Request("Page.close");

        public static ICDPRequest<FrameTree> GetFrameTree =>
            CDP.Request("Page.getFrameTree", new JObject(), paramsJson => new FrameTree((JObject)paramsJson["frameTree"]!));

        public static ICDPRequest Navigate(Uri url, int frameId = -1)
        {
            var jsonParams = new JObject();
            jsonParams.Add("url", url.ToString());
            if (frameId > -1)
                jsonParams.Add("frameId", frameId);
            return CDP.Request("Page.navigate", jsonParams, resultJson => (int)resultJson["frameId"]!);
        }

        public static ICDPRequest<int> CreateIsolatedWorld(int frameId, string? worldName = null)
        {
            var jsonParams = new JObject();
            jsonParams.Add("frameId", frameId);
            if (worldName != null)
                jsonParams.Add("worldName", worldName);
            return CDP.Request("Page.createIsolatedWorld", jsonParams, resultJson => (int)resultJson["executionContextId"]!);
        }
    }

    public struct FrameTree
    {
        public FrameInfo Frame;
        public FrameTree[] Children;

        public FrameTree(JObject frameTreeJson)
        {
            Frame = new FrameInfo((JObject)frameTreeJson["frame"]!);
            Children = frameTreeJson["childFrames"]!
                .Cast<JObject>()
                .Select(t => new FrameTree(t))
                .ToArray();
        }
    }

    public struct FrameInfo
    {
        public int Id;
        public Uri NavigationUri;

        public FrameInfo(JObject frameInfoJson)
        {
            Id = (int)frameInfoJson["id"]!;
            NavigationUri = new Uri(frameInfoJson["url"]!.ToString());
        }
    }
}
