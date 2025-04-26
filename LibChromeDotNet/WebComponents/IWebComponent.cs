using LibChromeDotNet.ChromeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.WebComponents
{
    public interface IWebComponent : IComponentResource
    {
        public Task LoadTemplateAsync(IWebTemplateSet templateSet, string templateName);
    }

    public interface IComponentResource
    {
        public void SubscribePropertyChanged(Action onPropertyChanged);
    }
}
