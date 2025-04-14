using LibChromeDotNet.CDP.Domains;
using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5;
using LibChromeDotNet.HTML5.CSS;
using LibChromeDotNet.HTML5.DOM;
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
            await OpenTestWindowAsync(context);
        }

        private async Task OpenTestWindowAsync(IAppContext context)
        {
            var window = await context.OpenWindowAsync();
            var docBody = await window.GetDocumentBodyAsync();
            var contentDivNode = await docBody.QuerySelectAsync("#content");
            var toggleButtonNode = await docBody.QuerySelectAsync("#toggle-rainbow-button");
            var contentClassList = await contentDivNode.GetClassListAsync();
            await toggleButtonNode.AddEventListenerAsync(MouseEvent.Click, async e =>
            {
                await contentClassList.ToggleAsync("rainbow");
            });
        }
    }
}
