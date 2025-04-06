using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public class ManifestResourceBank : IURIFetchHandler
    {
        public string UriPattern => $"{_Scheme}:///";

        public ManifestResourceBank(string uriScheme, Assembly sourceAssembly, string baseResourceName)
        {
            _Scheme = uriScheme;
            _SourceAssembly = sourceAssembly;
            _BaseResourceName = baseResourceName;
        }

        private string _Scheme;
        private Assembly _SourceAssembly;
        private string _BaseResourceName;
        private MemoryStream _MemoryStream = new MemoryStream();

        public async Task HandleAsync(IResourceFetchContext fetchContext)
        {
            var uri = fetchContext.Request.RequestUri;
            var resourceName = _BaseResourceName + uri.AbsolutePath.Replace('/', '.');
            using (var stream = _SourceAssembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    await fetchContext.FailRequestAsync(FailReason.NameNotResolved);
                    return;
                }
                byte[] resourceData = StreamToArray(stream);
                await fetchContext.FulfillRequestAsync(200, httpResponse => httpResponse.SetBody(resourceData));
            }
        }

        private byte[] StreamToArray(Stream stream) // save memory by reusing MemoryStream for instance
        {
            stream.CopyTo(_MemoryStream);
            var result = _MemoryStream.ToArray();
            _MemoryStream.Seek(0, SeekOrigin.Begin);
            _MemoryStream.SetLength(0);
            return result;
        }
    }
}
