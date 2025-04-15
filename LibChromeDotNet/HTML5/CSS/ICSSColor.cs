using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.CSS
{
    public interface ICSSColor
    {
        string Name { get; }
    }

    public class CSSColor : ICSSColor
    {
        public static ICSSColor FromRGBA(float r, float g, float b, float a)
        {
            var hexCode = $"#{(r * 255):X2}{(g * 255):X2}{(b * 255):X2}{(a * 255):X2}";
            return new CSSColor(hexCode);
        }

        public string Name => _Name;

        private CSSColor(string name)
        {
            _Name = name;
        }

        private string _Name;
    }
}
