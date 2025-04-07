// See https://aka.ms/new-console-template for more information
using LibChromeDotNet.CDP;
using LibChromeDotNet.ChromeApplication;
using LibChromeDotNet.ChromeInterop;
using System.Reflection;

var launcher = ChromeLauncher.CreateForPlatform();
//var resourceBank = new ManifestResourceBank("http", "appresource", Assembly.GetExecutingAssembly(), "ChromeDotNet_Test.sources");
var cdpSocket = new CDPSocket();
var browser = await launcher.LaunchAsync("https://www.google.com");

await cdpSocket.ConnectAsync(browser.CDPTarget, CancellationToken.None);
var interopSocket = new InteropSocket(cdpSocket);
var interopTargets = await interopSocket.GetTargetsAsync();
var openedWindow = interopTargets
    .Where(t => t.Type == DebugTargetType.Page)
    .First();
await Task.Delay(5000);
var session = await interopSocket.OpenSessionAsync(openedWindow);
//await session.EvaluateExpressionAsync("alert('hello')");
await interopSocket.CreateTargetAsync("about:blank", false);
Console.ReadLine();