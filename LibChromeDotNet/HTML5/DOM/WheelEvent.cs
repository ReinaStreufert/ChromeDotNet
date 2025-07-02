using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.DOM
{
    public class WheelEvent : IDOMEvent<WheelEventArgs>
    {
        public IEnumerable<string> SerializedProperties { get; } = new string[]
        {
            "deltaX",
            "deltaY",
            "deltaZ",
            "deltaMode"
        };
        public DOMEventType EventType => DOMEventType.Wheel;

        public WheelEventArgs GetParamsFromJson(JObject eventListenerArgs)
        {
            return new WheelEventArgs(eventListenerArgs);
        }
    }

    public struct WheelEventArgs
    {
        public double DeltaX;
        public double DeltaY;
        public double DeltaZ;
        public WheelDeltaMode Mode;

        public WheelEventArgs(JObject eventJson)
        {
            DeltaX = (double)eventJson["deltaX"]!;
            DeltaY = (double)eventJson["deltaY"]!;
            DeltaZ = (double)eventJson["deltaZ"]!;
            var modeCode = (int)eventJson["deltaMode"]!;
            Mode = (WheelDeltaMode)modeCode;
        }
    }

    public enum WheelDeltaMode
    {
        Pixels = 0,
        Lines = 1,
        Pages = 2
    }
}
