C# Library for creating local web apps. Interops directly with chrome, exposing DOM and javascript Web APIs as C# APIs.

* Write UIs in HTML/CSS and manipulate the frontend directly with C#, instead of with javascript
  + C# APIs for directly modifying DOM nodes
  + C# APIs which wrap common JS Web APIs such as event listeners, exposing them directly in C#
  + C# APIs which interop with the JS environment. Inject scripts and manipulate JS in C#

ChromeDotNet uses Chrome for Testing for stable and predictable browser behavior. It is also easy to download programmatically. ChromeDotNet uses CDP (Chrome DevTools Protocol) to send commands to the browser, similar to Pupeteer