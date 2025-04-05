using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.CDP.Domains
{
    public static class DOM
    {
        public static ICDPRequest<DOMNodeInfo> GetDocument =>
            CDP.Request("DOM.getDocument", new JObject(), resultJson => new DOMNodeInfo((JObject)resultJson["root"]!));

        public static ICDPRequest<DOMNodeTree> GetDocumentTree(int depth = -1)
        {
            var jsonParams = new JObject()
            {
                { "depth", depth }
            };
            return CDP.Request("DOM.getDocument", jsonParams, resultJson => new DOMNodeTree((JObject)resultJson["root"]!, depth));
        }

        public static ICDPRequest<IEnumerable<KeyValuePair<string, string>>> GetAttributes(int nodeId)
        {
            var jsonParams = new JObject()
            {
                { "nodeId", nodeId }
            };
            return CDP.Request("DOM.getAttributes", jsonParams, resultJson =>
                EnumerateInterleavedPairs(resultJson["attributes"]!.Select(t => t.ToString())));
        }

        public static ICDPRequest SetAttributeValue(int nodeId, string attributeName, string value)
        {
            var jsonParams = new JObject()
            {
                { "nodeId", nodeId },
                { "name", attributeName },
                { "value", value }
            };
            return CDP.Request("DOM.setAttributeValue", jsonParams);
        }

        public static ICDPRequest RemoveAttribute(int nodeId, string attributeName)
        {
            var jsonParams = new JObject()
            {
                { "nodeId", nodeId },
                { "name", attributeName }
            };
            return CDP.Request("DOM.removeAttribute", jsonParams);
        }

        public static ICDPRequest SetNodeValue(int nodeId, string nodeValue)
        {
            var jsonParams = new JObject()
            {
                { "nodeId", nodeId },
                { "value", nodeValue }
            };
            return CDP.Request("DOM.setNodeValue", jsonParams);
        }

        public static ICDPRequest<DOMNodeTree> GetNodeTree(int rootNodeId, int depth = -1)
        {
            var jsonParams = new JObject()
            {
                { "nodeId", rootNodeId }
            };
            return CDP.Request("DOM.describeNode", jsonParams, resultJson => new DOMNodeTree((JObject)resultJson["node"]!, depth));
        }

        public static ICDPRequest<int> QuerySelector(int nodeId, string selector)
        {
            var jsonParams = new JObject()
            {
                { "nodeId", nodeId },
                { "selector", selector }
            };
            return CDP.Request("DOM.querySelector", jsonParams, resultJson => (int)resultJson["nodeId"]!);
        }

        public static ICDPRequest<IEnumerable<int>> QuerySelectorMany(int nodeId, string selector)
        {
            var jsonParams = new JObject()
            {
                { "nodeId", nodeId },
                { "selector", selector }
            };
            return CDP.Request("DOM.querySelectorAll", jsonParams, resultJson => resultJson["nodeIds"]!.Cast<int>());
        }

        public static ICDPRequest RemoveNode(int nodeId)
        {
            var jsonParams = new JObject()
            {
                { "nodeId", nodeId }
            };
            return CDP.Request("DOM.removeNode", jsonParams);
        }

        public static ICDPRequest<RemoteObject> ResolveJavscriptNode(int nodeId, int executionContextId = -1)
        {
            var jsonParams = new JObject();
            jsonParams.Add("nodeId", nodeId);
            if (executionContextId > -1)
                jsonParams.Add("executionContextId", executionContextId);
            return CDP.Request("DOM.resolveNode", jsonParams, resultJson => new RemoteObject((JObject)resultJson["object"]!));
        }

        private static IEnumerable<KeyValuePair<string, string>> EnumerateInterleavedPairs(IEnumerable<string> interleavedPairs)
        {
            string? key = null;
            foreach (var interleavedItem in interleavedPairs)
            {
                if (key == null)
                    key = interleavedItem;
                else
                {
                    yield return new KeyValuePair<string, string>(key, interleavedItem);
                    key = null;
                }
            }
            if (key != null)
                throw new FormatException("Expected value after key");
        }
    }

    public struct DOMNodeInfo
    {
        public int Id;
        public string Name;
        public string Value;

        public DOMNodeInfo(JObject nodeJson)
        {
            Id = (int)nodeJson["nodeId"]!;
            Name = nodeJson["nodeName"]!.ToString();
            Value = nodeJson["nodeValue"]!.ToString();
        }
    }

    public struct DOMNodeTree
    {
        public DOMNodeInfo Node;
        public int Depth;
        public DOMNodeTree[]? Children;

        public DOMNodeTree(JObject nodeJson, int depth)
        {
            Node = new DOMNodeInfo(nodeJson);
            Depth = depth;
            if (depth > 0 || depth == -1)
            {
                var childrenJson = nodeJson["children"];
                if (childrenJson == null)
                    Children = new DOMNodeTree[0];
                else
                {
                    Children = childrenJson
                        .Cast<JObject>()
                        .Select(o => new DOMNodeTree(o, depth == -1 ? -1 : depth - 1))
                        .ToArray();
                }
            }
        }
    }
}
