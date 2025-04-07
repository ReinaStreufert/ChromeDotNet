// See https://aka.ms/new-console-template for more information
using ChromeDotNet_Test;
using LibChromeDotNet.CDP;
using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5;
using System.Reflection;

var app = new TestWebApp();
var appHost = WebAppHost.Create(app);
await appHost.LaunchAsync();
Console.ReadLine();