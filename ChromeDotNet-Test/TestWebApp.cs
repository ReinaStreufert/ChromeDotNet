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
            var currentTextHead = await docBody.QuerySelectAsync<HTMLTextElement>("#current-text-head");
            var textboxInput = await docBody.QuerySelectAsync<HTMLInputElement>("#textbox");
            var clearButtonNode = await docBody.QuerySelectAsync("#clear-button");
            textboxInput.ValueChanged += () => currentTextHead.Text = textboxInput.Value;
            await clearButtonNode.AddEventListenerAsync(MouseEvent.Click, e => textboxInput.Value = "");
        }
    }
}
