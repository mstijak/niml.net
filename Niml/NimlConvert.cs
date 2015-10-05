using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Niml
{
    public class NimlConvert
    {
        readonly NimlTextReader tr;

        private NimlConvert(TextReader r)
        {
            tr = new NimlTextReader(r);
        }

        IList<NObject> Convert()
        {
            NElement dummyParent = new NElement("dummy");

            var parents = new Stack<NElement>();
            parents.Push(dummyParent);
            
            NElement lastEl = dummyParent,
                     lastElParent = dummyParent, 
                     parentEl = null;

            bool addToLast = false;
            string lastAttributeName = null;

            while (tr.Read())
            {
                parentEl = lastEl != null && (tr.Nest || addToLast) ? lastEl : parents.Peek();

                addToLast = false;

                switch (tr.Token)
                {
                    case NimlToken.Element:
                        lastElParent = parentEl;
                        lastEl = new NElement(tr.Value);
                        parentEl.AddChild(lastEl);
                        break;


                    case NimlToken.InlineText:
                    case NimlToken.MultilineText:
                        lastEl.AddChildText(tr.Value);
                        break;

                    case NimlToken.Text:
                        var text = new NText(tr.Value);
                        parentEl.AddChild(text);
                        break;

                    case NimlToken.ExitElement:
                        if (parents.Count>0)
                        {
                            parents.Pop();
                            lastEl = null;
                        }
                        break;

                    case NimlToken.EnterElement:
                        parents.Push(lastEl);
                        break;

                    case NimlToken.EnterLast:
                        addToLast = true;
                        break;

                    case NimlToken.CloseLast:
                        lastEl = lastElParent;
                        break;
                    
                    case NimlToken.AttributeName:
                        lastAttributeName = tr.Value;
                        if (lastAttributeName == String.Empty)
                            break;

                        if (lastEl.Attributes == null)
                            lastEl.Attributes = new Dictionary<string, string>();

                        lastEl.Attributes.Add(lastAttributeName, null);
                        break;

                    case NimlToken.AttributeValue:
                        lastEl.Attributes[lastAttributeName] = tr.Value;
                        break;
                }
            }

            return dummyParent.Children ?? new List<NObject>();
        }

        public static IList<NObject> Convert(TextReader r)
        {
            var c = new NimlConvert(r);
            return c.Convert();
        }

        public static XDocument ToXDocument(TextReader r)
        {
            var c = new NimlConvert(r);
            var doc = new XDocument();
            foreach (var x in c.Convert())
                doc.Add(x.ToXNode());
            return doc;
        }
    }
}
