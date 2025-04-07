using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5
{
    public interface IWebApp
    {
        public IWebContent Content { get; }
        public Task OnStartupAsync(IAppContext context);
    }
}
