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
        public static ICSSColor FromRGBA(float r, float g, float b, float a = 1f)
        {
            var hexCode = $"#{GetByteColorVec(r):X2}{GetByteColorVec(g):X2}{GetByteColorVec(b):X2}{GetByteColorVec(a):X2}";
            return new CSSColor(hexCode);
        }

        private static int GetByteColorVec(float value) => (int)Math.Round(value * 255);

        public string Name => _Name;

        private CSSColor(string name)
        {
            _Name = name;
        }

        private string _Name;
    }
}
