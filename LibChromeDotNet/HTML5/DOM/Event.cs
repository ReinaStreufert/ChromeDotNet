using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.DOM
{
    public class Event : IDOMEvent
    {
        public static MouseEvent Click => new MouseEvent(DOMEventType.Click);
        public static MouseEvent DoubleClick => new MouseEvent(DOMEventType.DblClick);
        public static MouseEvent MouseUp = new MouseEvent(DOMEventType.MouseUp);
        public static MouseEvent MouseDown => new MouseEvent(DOMEventType.MouseDown);
        public static MouseEvent MouseMove => new MouseEvent(DOMEventType.MouseMove); // whoops, duh
        public static KeyboardEvent KeyDown => new KeyboardEvent(DOMEventType.KeyDown);
        public static KeyboardEvent KeyUp => new KeyboardEvent(DOMEventType.KeyUp);
        public static Event Change => new Event(DOMEventType.Change);
        public static Event Resize => new Event(DOMEventType.Resize);

        public static IDOMEvent FromEventType(DOMEventType eventType)
        {
            return eventType switch
            {
                DOMEventType.Click => Click,
                DOMEventType.DblClick => DoubleClick,
                DOMEventType.MouseDown => MouseDown,
                DOMEventType.MouseUp => MouseUp,
                DOMEventType.MouseMove => MouseMove,
                DOMEventType.KeyDown => KeyDown,
                DOMEventType.KeyUp => KeyUp,
                DOMEventType.Change => Change,
                DOMEventType.Resize => Resize,
                _ => throw new NotImplementedException()
            };
        }

        public static IDOMEvent FromEventName(string eventTypeName) => FromEventType(Enum.Parse<DOMEventType>(eventTypeName, true));

        public DOMEventType EventType { get; }

        private Event(DOMEventType eventType)
        {
            EventType = eventType;
        }
    }
}
