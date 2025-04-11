using ChromeDotNet_Test;
using LibChromeDotNet.CDP;
using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5;
using System.Reflection;

var app = new TestWebApp();
var appHost = WebCRAppHost.Create(app);
await appHost.LaunchAppAsync();