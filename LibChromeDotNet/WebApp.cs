using LibChromeDotNet.HTML5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet
{
    public class WebApp : IWebApp
    {
        public static WebApp CreateFromAssemblyManifest(string contentSrcPath, AppLaunchAsyncHandler handler)
        {
            var assembly = Assembly.GetCallingAssembly();
            var rootNamespace = assembly.EntryPoint?.DeclaringType?.Namespace;
            if (rootNamespace == null)
                throw new InvalidOperationException("The assembly calling this constructor must have an entry namespace");
            var manifestResourcePrefix = $"{rootNamespace}.{contentSrcPath.TrimStart('/').Replace('/', '.')}";
            var content = new WebContent();
            content.AddManifestSources("/", assembly, manifestResourcePrefix);
            content.SetIndex();
            return new WebApp(content, handler);

        }

        IWebContent IWebApp.Content => _Content;

        private WebApp(IWebContent content, AppLaunchAsyncHandler handler)
        {
            _Content = content;
            _LaunchHandler = handler;
        } 

        private IWebContent _Content;
        private AppLaunchAsyncHandler _LaunchHandler;

        async Task IWebApp.OnStartupAsync(IAppContext context)
        {
            await _LaunchHandler(context);
        }
    }

    public delegate Task AppLaunchAsyncHandler(IAppContext context);
}
