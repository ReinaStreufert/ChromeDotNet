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
    public interface IJSModule
    {
        public string Name { get; }
        public Task<string> GetScriptSourceAsync();
    }
}
