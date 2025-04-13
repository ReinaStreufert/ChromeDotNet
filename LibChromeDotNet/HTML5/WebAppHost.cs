using LibChromeDotNet.ChromeApplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5
{
    public class WebAppHost : IWebAppHost
    {
        public static IWebAppHost Create(IWebApp app) => new WebAppHost(ChromeLauncher.CreateForPlatform(), app);

        private WebAppHost(IChromeLauncher launcher, IWebApp app)
        {
            _App = app;
            _Launcher = launcher;
        }

        private IWebApp _App;
        private IChromeLauncher _Launcher;
        private IWebContentHost _ContentHost = new WebContentHost();

        public async Task LaunchAppAsync()
        {
            var listenTask = _ContentHost.ListenAsync(_App.Content);
            var context = new WebAppContext(_Launcher, _ContentHost);
            await _App.OnStartupAsync(context);
            await listenTask;
        }
    }
}
