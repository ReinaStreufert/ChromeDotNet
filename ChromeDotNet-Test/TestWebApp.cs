using LibChromeDotNet.CDP.Domains;
using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5;
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
            var currentPosHead = await docBody.QuerySelectAsync<HTMLTextElement>("#current-pos-head");
            var clickPosHead = await docBody.QuerySelectAsync<HTMLTextElement>("#click-pos-head");
            var contentDiv = await docBody.QuerySelectAsync("#content");
            await contentDiv.AddEventListenerAsync(MouseEvent.Click, e =>
            {
                clickPosHead.Text = $"You clicked at: ({e.ClientX},{e.ClientY})";
            });
            await contentDiv.AddEventListenerAsync(MouseEvent.MouseMove, e =>
            {
                currentPosHead.Text = $"Your pointer is at: ({e.ClientX},{e.ClientY})";
            });

            var openButton = await docBody.QuerySelectAsync("#open-button");
            var closeButton = await docBody.QuerySelectAsync("#close-button");
            await openButton.AddEventListenerAsync(MouseEvent.Click, async (e) => await OpenTestWindowAsync(context));
            await closeButton.AddEventListenerAsync(MouseEvent.Click, async (e) => await window.CloseAsync());
        }
    }
}
