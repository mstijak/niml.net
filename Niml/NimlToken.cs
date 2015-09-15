using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Niml
{
    public enum NimlToken
    {
        None,

        StartTag,        
        EndElement,

        EnterElement,
        AddToLastElement,
        IndentDecrease,

        InlineText,
        MultilineText,
        AttributeName,
        AttributeValue,
        Text
    }
}
