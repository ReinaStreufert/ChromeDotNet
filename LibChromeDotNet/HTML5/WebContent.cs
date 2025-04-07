using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5
{
    public class WebContent : IWebContent
    {
        private List<IContentSource> _Resources = new List<IContentSource>();

        public WebContent()
        {

        }

        private IContentSource? _IndexResource;

        private IContentSource? GetResource(string path)
        {
            var trimmedPath = path.TrimStart('/');
            var resource = _Resources
                .Where(s => s.Path.TrimStart('/') == trimmedPath)
                .FirstOrDefault();
            return resource;
        }

        public Task<IContentSource> GetIndexResourceAsync()
        {
            if (_IndexResource == null)
                throw new InvalidOperationException("Index resource is unset");
            return Task.FromResult(_IndexResource); // interface supports async, implementation doesnt need it
        }

        public Task<IContentSource?> GetResourceAsync(string path) => Task.FromResult(GetResource(path));

        public void SetIndex(string path = "index.html")
        {
            var resource = GetResource(path);
            if (resource == null)
                throw new ArgumentException($"No source has been added at the specified path");
            _IndexResource = resource;
        }

        public void AddSource(string path, string mimeType, string sourceText)
        {
            _Resources.Add(new TextContentSource(path, mimeType, sourceText));
        }

        public void AddFileSource(string path, string filePath, string? mimeType = null)
        {
            if (mimeType == null)
                mimeType = MimeMapping.GetMimeMapping(filePath);
            _Resources.Add(new FileContentSource(path, mimeType, filePath));
        }

        public void AddFileSources(string basePath, string baseDirectory)
        {
            if (!basePath.EndsWith('/'))
                basePath = basePath + "/";
            foreach (var filePath in Directory.GetFiles(baseDirectory))
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var contentSrcPath = basePath + fileName;
                AddFileSource(contentSrcPath, filePath);
            }
            foreach (var dirPath in Directory.GetDirectories(baseDirectory))
            {
                var dirName = Path.GetFileName(dirPath);
                AddFileSources(basePath + dirName, dirPath);
            }
        }

        public void AddManifestSource(string basePath, Assembly srcAssembly, string resourceName, string? mimeType = null)
        {
            if (mimeType == null)
                mimeType = MimeMapping.GetMimeMapping(PathFromManifestResourceName(resourceName));
            _Resources.Add(new ManifestContentSource(basePath, mimeType, srcAssembly, resourceName));
        }

        public void AddManifestSources(string basePath, Assembly srcAssembly, string baseResourcePath)
        {
            if (!basePath.EndsWith('/'))
                basePath = basePath + "/";
            var matchingResourceNames = srcAssembly.GetManifestResourceNames()
                .Where(n => n.StartsWith(baseResourcePath));
            foreach (var resourceName in matchingResourceNames)
            {
                var relativeResourcePath = PathFromManifestResourceName(resourceName)
                    .Substring(baseResourcePath.Length + 1);
                AddManifestSource(basePath + relativeResourcePath, srcAssembly, resourceName);
            }
        }

        private string PathFromManifestResourceName(string resourceName)
        {
            var split = resourceName.Split('.');
            if (split.Length == 0)
                return string.Empty;
            var ext = split[split.Length - 1];
            var path = string.Join('/', split.Take(split.Length - 1));
            return $"{path}.{ext}";
        }

        private class TextContentSource : IContentSource
        {
            public string Path { get; }
            public string MimeType { get; }

            public TextContentSource(string path, string mimeType, string content)
            {
                Path = path;
                MimeType = mimeType;
                _Content = content;
            }

            private string _Content;

            public void WriteToStream(Stream destStream)
            {
                using (StreamWriter sw = new StreamWriter(destStream))
                    sw.Write(_Content);
            }
        }

        private class FileContentSource : IContentSource
        {
            public string Path { get; }
            public string MimeType { get; }

            public FileContentSource(string path, string mimeType, string srcFilePath)
            {
                Path = path;
                MimeType = mimeType;
                _SrcFilePath = srcFilePath;
            }

            private string _SrcFilePath;

            public void WriteToStream(Stream destStream)
            {
                using (FileStream fs = new FileStream(_SrcFilePath, FileMode.Open, FileAccess.Read))
                    fs.CopyTo(destStream);
            }
        }

        private class ManifestContentSource : IContentSource
        {
            public string Path { get; }
            public string MimeType { get; }

            public ManifestContentSource(string path, string mimeType, Assembly assembly, string resourceName)
            {
                Path = path;
                MimeType = mimeType;
                _Assembly = assembly;
                _ResourceName = resourceName;
            }

            private Assembly _Assembly;
            private string _ResourceName;

            public void WriteToStream(Stream destStream)
            {
                using (var resourceStream = _Assembly.GetManifestResourceStream(_ResourceName))
                {
                    if (resourceStream == null)
                        throw new InvalidOperationException();
                    resourceStream.CopyTo(destStream);
                }
            }
        }
    }
}
