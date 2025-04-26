using LibChromeDotNet.HTML5;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet
{
    public abstract class WebApp : IWebApp
    {
        private WebContent _Content;

        protected WebApp(string contentSrcPath)
        {
            var assembly = Assembly.GetCallingAssembly();
            var rootNamespace = assembly.EntryPoint?.DeclaringType?.Namespace;
            if (rootNamespace == null)
                throw new InvalidOperationException("The assembly calling this constructor must have an entry namespace");
            var manifestResourcePrefix = $"{rootNamespace}.{contentSrcPath.TrimStart('/').Replace('/', '.')}";
            var content = new WebContent();
            content.AddManifestSources("/", assembly, manifestResourcePrefix);
            
            _Content = content;
        }

        protected WebApp(string contentSrcPath, string indexPageName) : this(contentSrcPath)
        {
            _Content.SetIndex(indexPageName);
        }

        protected abstract Task OnStartupAsync(IAppContext context);

        IWebContent IWebApp.Content => _Content;

        async Task IWebApp.OnStartupAsync(IAppContext context)
        {
            await OnStartupAsync(context);
        }
    }
}
