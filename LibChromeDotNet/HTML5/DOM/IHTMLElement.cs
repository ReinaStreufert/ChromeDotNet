using LibChromeDotNet.ChromeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.DOM
{
    public interface IHTMLElement : IAsyncDisposable
    {
        public IDOMNode Node { get; }
    }
}
