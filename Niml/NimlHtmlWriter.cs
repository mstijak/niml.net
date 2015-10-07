using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace Niml
{
    public class NimlHtmlWriter : IDisposable
    {
        readonly HtmlTextWriter w;

        public NimlHtmlWriter(TextWriter w)
        {
            this.w = new HtmlTextWriter(w);
        }

        public void Dispose()
        {
            w.Dispose();
        }

        public void Write(NObject o)
        {
            switch (o.ObjectType)
            {
                case NimlObjectType.Text:
                    WriteText((NText)o);
                    break;

                case NimlObjectType.Element:
                    WriteElement((NElement)o);
                    break;
            }
        }

        private void WriteElement(NElement o)
        {
            if (o.Attributes != null)
                foreach (var kv in o.Attributes)
                    w.AddAttribute(kv.Key, kv.Value);

            w.RenderBeginTag(o.Name);

            if (o.Children != null)
                foreach (var child in o.Children)
                    Write(child);

            w.RenderEndTag();
        }

        private void WriteText(NText o)
        {
            if (o.Raw)
                w.Write(o.Value);
            else
                w.WriteEncodedText(o.Value);
        }
    }
}
