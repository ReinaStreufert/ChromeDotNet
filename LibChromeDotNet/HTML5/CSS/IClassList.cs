using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.CSS
{
    public interface IClassList
    {
        public Task<bool> ToggleAsync(string className);
        public Task AddAsync(string className);
        public Task RemoveAsync(string className);
        public Task ContainsAsync(string className);
    }
}
