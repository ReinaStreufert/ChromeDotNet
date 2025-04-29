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
        protected WebContent Content;

        protected WebApp(string contentSrcPath, string? indexPageName = null)
        {
            var assembly = Assembly.GetCallingAssembly();
            var rootNamespace = assembly.EntryPoint?.DeclaringType?.Namespace;
            if (rootNamespace == null)
                throw new InvalidOperationException("The assembly calling this constructor must have an entry namespace");
            var manifestResourcePrefix = $"{rootNamespace}.{contentSrcPath.TrimStart('/').Replace('/', '.')}";
            var content = new WebContent();
            content.AddManifestSources("/", assembly, manifestResourcePrefix);
            if (indexPageName != null)
                content.SetIndex();
            Content = content;
        }

        protected abstract Task OnStartupAsync(IAppContext context);

        IWebContent IWebApp.Content => Content;

        async Task IWebApp.OnStartupAsync(IAppContext context)
        {
            await OnStartupAsync(context);
        }
    }
}
