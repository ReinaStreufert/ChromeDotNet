using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    // provides an API for anonymous JS scripts to store a persistent recallable result
    // for example, the entire HTML5 namespace of extension methods is implemented
    // using IJSModule ensuring interop scripts are only executed once
    public interface IJSModule<TKey> where TKey : class
    {
        public TKey Key { get; }
        public Task<string> GetScriptSourceAsync();
    }

    public interface IJSModule : IJSModule<object> { }
    public interface IJSNamedModule : IJSModule<string> { }
}
