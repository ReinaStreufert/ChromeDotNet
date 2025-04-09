using LibChromeDotNet.ChromeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.DOM
{
    public class ManifestJSModule : IJSModule
    {
        public static IJSModule FromScriptName(string scriptName) =>
            new ManifestJSModule(scriptName);

        private const string ManifestResourceBase = "LibChromeDotNet.HTML5.DOM.jsScripts";

        public string Name { get; }

        public ManifestJSModule(string name)
        {
            Name = name;
        }

        public async Task<string> GetScriptSourceAsync()
        {
            var asm = Assembly.GetExecutingAssembly();
            var manifestResourceName = $"{ManifestResourceBase}.{Name}";
            using (var resourceStream = asm.GetManifestResourceStream(manifestResourceName) ?? throw new InvalidOperationException())
            using (var reader = new StreamReader(resourceStream))
                return await reader.ReadToEndAsync();
        }
    }
}
