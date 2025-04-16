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
        private object _Sync = new object();
        private Task? _RainbowTask;
        private CancellationTokenSource _RainbowCancelSource = new CancellationTokenSource();

        public async Task OnStartupAsync(IAppContext context)
        {
            await OpenTestWindowAsync(context);
        }

        private async Task OpenTestWindowAsync(IAppContext context)
        {
            var window = await context.OpenWindowAsync();
            var docBody = await window.GetDocumentBodyAsync();
            var heading = await docBody.QuerySelectAsync<HTMLTextElement>("#heading");
            var toggleColorsButton = await docBody.QuerySelectAsync("#toggle-colors");
            var msg = "the.quick.brown.fox.jumps.over.the.lazy.dog";
            var rainbowColors = new ICSSColor[]
            {
                CSSColor.FromRGBA(1f, 0f, 0f), // red
                CSSColor.FromRGBA(1f, 0.7f, 0f), // orange
                CSSColor.FromRGBA(1f, 1f, 0f), // yellow
                CSSColor.FromRGBA(0.3f, 1f, 0f), // green
                CSSColor.FromRGBA(0f, 0.3f, 1f), // teal-ish blue
                CSSColor.FromRGBA(0.7f, 0f, 1f) // purple
            };

            await toggleColorsButton.AddEventListenerAsync(MouseEvent.Click, e =>
            {
                lock (_Sync)
                {
                    if (_RainbowTask == null)
                        _RainbowTask = AnimateTextAsync(heading, msg, _RainbowCancelSource.Token, rainbowColors);
                    else
                    {
                        _RainbowCancelSource.Cancel();
                        _RainbowCancelSource = new CancellationTokenSource();
                        _RainbowTask = null;
                    }
                }
            });
        }

        private static async Task AnimateTextAsync(HTMLTextElement textElement, string text, CancellationToken cancelToken, params ICSSColor[] colors)
        {
            var offset = 0;
            while (!cancelToken.IsCancellationRequested)
            {
                FormattedTextNode? firstNode = null;
                FormattedTextNode? lastNode = null;
                for (int i = 0; i < text.Length; i++)
                {
                    var color = colors[(i + offset) % colors.Length];
                    var style = ((i + offset) % 2 > 0) ? FontStyle.Strikethrough : FontStyle.Regular | FontStyle.Underline;
                    var node = new FormattedTextNode(text[i].ToString(), style, color);
                    if (lastNode == null)
                        firstNode = node;
                    else
                        lastNode.InsertAfter(node);
                    lastNode = node;
                }
                if (firstNode == null)
                    throw new ArgumentException(nameof(text));
                var formattedText = new FormattedText(firstNode);
                await textElement.SetTextAsync(formattedText);
                await Task.Delay(100);
                offset = (offset + 1) % colors.Length;
            }
        }
    }
}
