using LibChromeDotNet;

namespace CDN_WebAppTemplate
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await WebApp.CreateFromAssemblyManifest("web", new App().OnStartupAsync).LaunchAsync();
        }
    }
}