using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Niml
{
    static class CharUtil
    {
        public static bool IsWhitespace(this char c)
        {
            return c == ' ' || c == '\n' || c == '\r';
        }
    }
}
