using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.JSInterop
{
    public interface IDOMEvent<TParams>
    {
        string EventTypeName { get; }
        TParams GetParamsFromJson(JObject eventListenerArgs);
    }
}
