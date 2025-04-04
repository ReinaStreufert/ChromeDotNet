using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.WebInterop
{
    public interface IInteropSession
    {
        public IInteropSocket Socket { get; }
        public IInteropTarget Target { get; }
        public Task DetachAsync();
        public Task ClosePageAsync();
        public Task<IFrame> GetRootFrameAsync();
        public Task<IDOMNode> GetDOMDocumentAsync();
        public Task<IJSExecutionContext> CreateJSContextAsync();
    }
}
