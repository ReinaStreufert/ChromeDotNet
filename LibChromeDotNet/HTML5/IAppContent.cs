using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5
{
    public interface IAppContent
    {
        public Task<IContentSource> GetIndexResourceAsync();
        public Task<IContentSource?> GetResourceAsync(string path);
    }

    public interface IContentSource
    {
        public Stream GetContentStream();
        public ContentType MimeType { get; }
    }
}
