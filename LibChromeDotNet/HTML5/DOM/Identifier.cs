using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.HTML5.DOM
{
    public static class Identifier
    {
        private const int Length = 16;
        private const char Min = 'a';
        private const char Max = 'z';

        private static Random _Rand = new Random();

        public static string New()
        {
            var arr = new char[16];
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = (char)_Rand.Next(Min, Max + 1);
            }
            return new string(arr);
        }
    }
}
