using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.DOM
{
    public class KeyboardEvent : IDOMEvent<KeyboardEventArgs>
    {
        public static KeyboardEvent KeyDown => new KeyboardEvent("keydown");
        public static KeyboardEvent KeyUp => new KeyboardEvent("keyup");

        public string EventTypeName { get; }

        private KeyboardEvent(string name)
        {
            EventTypeName = name;
        }

        public KeyboardEventArgs GetParamsFromJson(JObject eventListenerArgs) => new KeyboardEventArgs(eventListenerArgs);
    }

    public struct KeyboardEventArgs
    {
        public ModifierKeys Modifiers;
        public string? Key;
        public string? Code;
        public bool? Repeat;

        public KeyboardEventArgs(JObject eventJson)
        {
            if (eventJson.ContainsKey("key"))
            {
                // something is wack but im ignoring it for now bc i dont need any of these event arguments yet.
                Modifiers = ModifierKeys.None;
                if ((bool)eventJson["altKey"]!)
                    Modifiers |= ModifierKeys.AltKey;
                if ((bool)eventJson["ctrlKey"]!)
                    Modifiers |= ModifierKeys.CtrlKey;
                if ((bool)eventJson["metaKey"]!)
                    Modifiers |= ModifierKeys.MetaKey;
                Key = eventJson["key"]!.ToString();
                Code = eventJson["code"]!.ToString();
                Repeat = (bool)eventJson["repeat"]!;
            }
        }
    }

    [Flags]
    public enum ModifierKeys
    {
        None = 0,
        CtrlKey = 1,
        AltKey = 2,
        MetaKey = 4
    }
}
