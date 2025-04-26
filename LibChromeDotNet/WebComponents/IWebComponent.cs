using LibChromeDotNet.ChromeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.WebComponents
{
    public interface IWebComponent
    {
        public Task SetTargetAsync(IDOMNode domNode);
        public Task RenderAsync();
    }
}
