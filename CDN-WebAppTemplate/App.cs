using LibChromeDotNet;
using LibChromeDotNet.HTML5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDN_WebAppTemplate
{
    public class App : WebApp
    {
        public App() : base("web/", "index.html")
        {
        }

        protected override async Task OnStartupAsync(IAppContext context)
        {
            var window = await context.OpenWindowAsync();
        }
    }
}
