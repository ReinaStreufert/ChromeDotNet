using LibChromeDotNet.CDP.Domains;
using LibChromeDotNet.ChromeInterop;
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
            appWindow.PageLoaded += AppWindow_PageLoaded;
        }

        private async Task AppWindow_PageLoaded(IAppWindow window)
        {
            var docBody = await window.GetDocumentBodyAsync();
            var headingNode = await docBody.QuerySelectAsync("#heading");
            var headingText = (await headingNode.GetChildrenAsync())
                .Where(n => n.NodeType == DOMNodeType.Text)
                .First();
            _ = CountAsync(headingText);
        }

        private async Task CountAsync(IDOMNode textNode)
        {
            var count = 0;
            for (; ;)
            {
                await Task.Delay(1000);
                count++;
                await textNode.SetValueAsync($"test: {count}");
            }
        }
    }
}
