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

        Element,
        EndElement,

        EnterElement,
        ExitElement,

        EnterLast,
        CloseLast,

        InlineText,
        MultilineText,
        AttributeName,
        AttributeValue,
        Text
    }
}
