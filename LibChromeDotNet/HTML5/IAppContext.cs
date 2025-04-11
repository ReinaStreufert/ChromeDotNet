using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5
{
    public interface IAppContext
    {
        public Task<IAppWindow> OpenWindowAsync(string contentPath = "/");
        public Task ExitAsync();
    }
}
