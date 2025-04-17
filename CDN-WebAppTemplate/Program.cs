using LibChromeDotNet;

namespace CDN_WebAppTemplate
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await new App().LaunchAsync();
        }
    }
}