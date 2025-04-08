using LibChromeDotNet.CDP.Domains;
using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5;
using LibChromeDotNet.HTML5.JSInterop;
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
        private int _Counter = 0;

        public async Task OnStartupAsync(IAppContext context)
        {
            var window = await context.OpenWindowAsync();
            var docBody = await window.GetDocumentBodyAsync();
            var headingNode = await docBody.QuerySelectAsync("#heading");
            var headingText = (await headingNode.GetChildrenAsync())
                .Where(n => n.NodeType == DOMNodeType.Text)
                .First();
            var textInput = await docBody.QuerySelectAsync("#textBox");
            await textInput.AddEventListenerAsync(GenericDOMEvent.Change, async () =>
            {
                await textInput.GetChildrenAsync();
            });
        }
    }
}
