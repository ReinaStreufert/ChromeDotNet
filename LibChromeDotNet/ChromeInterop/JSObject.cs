using LibChromeDotNet.CDP.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.ChromeInterop
{
    public class JSObject : IJSObject
    {
        private InteropSession _Session;
        private RemoteObject _ObjectInfo;

        public JSObject(InteropSession session, RemoteObject objectInfo)
        {
            _Session = session;
            _ObjectInfo = objectInfo;
        }

        public JSType Type => throw new NotImplementedException();

        public Task<IJSObject?> CallFunctionAsync(string name, params IJSObject[] arguments)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetKeysAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IJSProperty>> GetPropertiesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IJSObject>> GetValuesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
