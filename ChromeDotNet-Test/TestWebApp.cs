using LibChromeDotNet.HTML5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ChromeDotNet_Test
{
    public class TestWebApp : IWebApp
    {
        public IWebContent Content => _Content;

        public TestWebApp()
        {
            _Content.AddManifestSources("/", Assembly.GetExecutingAssembly(), "ChromeDotNet_Test.webSources");
            _Content.SetIndex();
        }

        private WebContent _Content = new WebContent();

        public async Task OnStartupAsync(IAppContext context)
        {
            var appWindow = await context.OpenWindow();
        }
    }
}
