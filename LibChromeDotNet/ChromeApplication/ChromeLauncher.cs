using LibChromeDotNet.CDP;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeApplication
{
    public static class ChromeLauncher
    {
        public const string UsedChromeRelease = "135.0.7049.42";

        public static IChromeLauncher CreateForPlatform(string chromeVersion = UsedChromeRelease)
        {
            var arch = RuntimeInformation.OSArchitecture;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ValidateArchitecture(arch, Architecture.X64, Architecture.X64);
                return new WinChromeLauncher(chromeVersion, arch);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ValidateArchitecture(arch, Architecture.X64, Architecture.Arm64);
                return new OSXChromeLauncher(chromeVersion, arch);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ValidateArchitecture(arch, Architecture.X64);
                return new LinuxChromeLauncher(chromeVersion);
            }
            else
                throw new PlatformNotSupportedException($"The OS is unsupported");
        }

        private static void ValidateArchitecture(Architecture osArch, params Architecture[] compatibleArchList)
        {
            foreach (var compatibleArch in compatibleArchList)
            {
                if (compatibleArch == osArch)
                    return;
            }
            throw new PlatformNotSupportedException($"The device architecture is unsupported");
        }
    }

    abstract class PlatformIndependentLauncher : IChromeLauncher
    {
        private const string BaseReleasesUri = "https://storage.googleapis.com/chrome-for-testing-public/135.0.7049.42";
        private const string DevPortAnnouncement = "DevTools listening on ";

        public string ReleaseVersion { get; }
        public Architecture ReleaseArchitecture { get; }
        public abstract OSPlatform ReleasePlatform { get; }
        protected abstract string DownloadPlatformId { get; }
        protected abstract string GetLaunchPath(string baseArchivePath);
        public bool IsInstalled => File.Exists(GetInstallPath());

        public PlatformIndependentLauncher(string releaseVersion, Architecture releaseArchitecture)
        {
            ReleaseVersion = releaseVersion;
            ReleaseArchitecture = releaseArchitecture;
        }

        public async Task EnsureInstalledAsync()
        {
            if (!IsInstalled)
            {
                using (HttpClient http = new HttpClient())
                {
                    var platformId = DownloadPlatformId;
                    var releaseUri = $"{BaseReleasesUri}/{platformId}/chrome-{platformId}.zip";
                    var downloadStream = await http.GetStreamAsync(releaseUri);
                    var installPath = GetInstallPath();
                    Directory.CreateDirectory(installPath);
                    using (var zipArchive = new ZipArchive(downloadStream, ZipArchiveMode.Read))
                        zipArchive.ExtractToDirectory(installPath);
                }
            }
        }

        public async Task<IBrowser> LaunchAsync(string uri, int debugPort)
        {
            await EnsureInstalledAsync();
            var launchPath = GetLaunchPath(GetInstallPath());
            var processParams = new StringBuilder();
            processParams.Append("--enable-logging --new-window --disable-infobars ");
            processParams.Append($"--app={uri} ");
            processParams.Append($"--remote-debugging-port={debugPort}");
            var psi = new ProcessStartInfo(launchPath, processParams.ToString());
            psi.RedirectStandardError = true;
            psi.UseShellExecute = false;
            var process = Process.Start(psi);
            if (process == null)
                throw new InvalidOperationException();
            Uri devPortUri;
            for (; ;)
            {
                var logOutputLine = await process.StandardError.ReadLineAsync();
                if (logOutputLine == null)
                {
                    process.Kill();
                    throw new InvalidOperationException("Chrome did not provide remote debugging port in standard error");
                }
                if (logOutputLine.StartsWith(DevPortAnnouncement))
                {
                    devPortUri = new Uri(logOutputLine.Substring(DevPortAnnouncement.Length));
                    break;
                }
            }
            var remoteHost = new CDPRemoteHost(devPortUri, ReleaseVersion);
            return new Browser(process, remoteHost);
        }

        private string GetInstallPath()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                $"libchrome-{ReleaseVersion}-{ReleasePlatform}-{ReleaseArchitecture}");
        }
    }

    class WinChromeLauncher : PlatformIndependentLauncher
    {
        public WinChromeLauncher(string releaseVersion, Architecture releaseArchitecture) : base(releaseVersion, releaseArchitecture)
        {
            
        }

        public override OSPlatform ReleasePlatform => OSPlatform.Windows;
        protected override string DownloadPlatformId => ReleaseArchitecture == Architecture.X64 ? "win64" : "win32";

        protected override string GetLaunchPath(string baseArchivePath)
        {
            return Path.Combine(baseArchivePath, $"chrome-{DownloadPlatformId}\\chrome.exe");
        }
    }

    class OSXChromeLauncher : PlatformIndependentLauncher
    {
        public OSXChromeLauncher(string releaseVersion, Architecture releaseArchitecture) : base(releaseVersion, releaseArchitecture)
        {
        }

        public override OSPlatform ReleasePlatform => OSPlatform.OSX;
        protected override string DownloadPlatformId => $"mac-{ReleaseArchitecture.ToString().ToLower()}";

        protected override string GetLaunchPath(string baseArchivePath)
        {
            return Path.Combine(baseArchivePath, $"chrome-{DownloadPlatformId}/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing");
        }
    }

    class LinuxChromeLauncher : PlatformIndependentLauncher
    {
        public LinuxChromeLauncher(string releaseVersion) : base(releaseVersion, Architecture.X64)
        {
        }

        public override OSPlatform ReleasePlatform => OSPlatform.Linux;
        protected override string DownloadPlatformId => "linux64";

        protected override string GetLaunchPath(string baseArchivePath)
        {
            return Path.Combine(baseArchivePath, $"{DownloadPlatformId}/chrome");
        }
    }
}
