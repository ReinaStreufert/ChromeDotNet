using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.CSS
{
    public interface ICSSClassList : IAsyncDisposable
    {
        public Task<bool> ToggleAsync(string className);
        public Task<bool> ContainsAsync(string className);
        public Task AddAsync(string className);
        public Task RemoveAsync(string className);
    }
}
