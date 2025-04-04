using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeForTesting
{
    public interface IChromeLauncher
    {
        public string ReleaseVersion { get; }
        public OSPlatform ReleasePlatform { get; }
        public Architecture ReleaseArchitecture { get; }
        public bool IsInstalled { get; }
        public Task EnsureInstalledAsync();
        public Task<IBrowser> LaunchAsync(string uri, int debugPort);
    }
}
