// See https://aka.ms/new-console-template for more information
using ChromeDotNet_Test;
using LibChromeDotNet.CDP;
using LibChromeDotNet.ChromeApplication;
using LibChromeDotNet.ChromeInterop;
using System.Reflection;

var app = new TestWebApp();
var appHost = WebAppHost.Create(app);
await appHost.LaunchAsync();
Console.ReadLine();