﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5
{
    public interface IWebContent
    {
        public Task<IContentSource> GetIndexResourceAsync();
        public Task<IContentSource?> GetResourceAsync(string path);
    }

    public interface IContentSource
    {
        public string Path { get; }
        public string MimeType { get; }
        public void WriteToStream(Stream destStream);
    }
}
