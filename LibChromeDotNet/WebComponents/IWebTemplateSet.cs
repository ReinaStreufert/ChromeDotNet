using LibChromeDotNet.WebComponents.ILGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LibChromeDotNet.WebComponents
{
    public interface IWebTemplateSet
    {
        public void IncludeTemplate(XmlDocument templateDescription);
        public void IncludeTemplates(IEnumerable<XmlDocument> templateDescriptions);
        public bool IsTemplateIncluded(string name);
        public bool IsTemplateLoaded(string name);
        public SubstituterInfo LoadTemplate(string name);
    }
}
