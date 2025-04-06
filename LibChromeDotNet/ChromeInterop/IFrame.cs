using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public interface IFrame : IInteropObject
    {
        public int Id { get; }
        public Uri NavigationUrl { get; }
        public IEnumerable<IFrame> Children { get; }
        public Task NavigateAsync(Uri url);
        public Task NavigateAsync(string url);

        public static bool IsChildFrame(IFrame containerFrame, int frameId)
        {
            if (containerFrame.Id == frameId)
                return true;
            foreach (var child in containerFrame.Children)
            {
                if (IsChildFrame(child, frameId))
                    return true;
            }
            return false;
        }

        public static bool IsChildFrame(IFrame containerFrame, IFrame frame) =>
            IsChildFrame(containerFrame, frame.Id);
    }
}
