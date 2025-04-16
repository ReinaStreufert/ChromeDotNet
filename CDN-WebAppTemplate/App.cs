using LibChromeDotNet.HTML5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDN_WebAppTemplate
{
    public class App
    {
        public async Task OnStartupAsync(IAppContext context)
        {
            await context.OpenWindowAsync();
        }
    }
}
