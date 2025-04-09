using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.DOM
{
    public class MouseEvent : IDOMEvent<MouseEventArgs>
    {
        public static MouseEvent Click => new MouseEvent("click");
        public static MouseEvent DoubleClick => new MouseEvent("dblclick");
        public static MouseEvent MouseUp = new MouseEvent("mouseup");
        public static MouseEvent MouseDown => new MouseEvent("mousedown");
        public static MouseEvent MouseMove => new MouseEvent("mousemove"); // whoops, duh

        public string EventTypeName { get; }
        public IEnumerable<string> SerializedProperties { get; } = new string[]
        {
            "clientX",
            "clientY",
            "movementX",
            "movementY",
            "pageX",
            "pageY",
            "screenX",
            "screenY"
        };

        private MouseEvent(string eventTypeName)
        {
            EventTypeName = eventTypeName;
        }

        public MouseEventArgs GetParamsFromJson(JObject eventListenerArgs)
        {
            return new MouseEventArgs(eventListenerArgs);
        }
    }

    public struct MouseEventArgs
    {
        public double ClientX;
        public double ClientY;
        public double MovementX;
        public double MovementY;
        public double PageX;
        public double PageY;
        public double ScreenX;
        public double ScreenY;

        public MouseEventArgs(JObject eventJson)
        {
            if (eventJson.ContainsKey("clientX")) // not all mouse events come with location (probably a lie.)
            {
                ClientX = (double)eventJson["clientX"]!;
                ClientY = (double)eventJson["clientY"]!;
                MovementX = (double)eventJson["movementX"]!;
                MovementY = (double)eventJson["movementY"]!;
                PageX = (double)eventJson["pageX"]!;
                PageY = (double)eventJson["pageY"]!;
                ScreenX = (double)eventJson["screenX"]!;
                ScreenY = (double)eventJson["screenY"]!;
            }
        }
    }
}
