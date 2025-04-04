using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.WebInterop
{
    public interface IFrame
    {
        Uri NavigationUrl { get; }
        IEnumerable<IFrame> Children { get; }
        Task NavigateAsync(Uri url);
    }
}
