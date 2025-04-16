using LibChromeDotNet.HTML5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet
{
    public static class WebAppExtensions
    {
        public static async Task LaunchAsync(this IWebApp app)
        {
            var appHost = WebAppHost.Create(app);
            await appHost.LaunchAppAsync();
        }
    }
}
