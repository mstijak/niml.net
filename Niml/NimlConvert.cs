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

            NElement currentElement = dummyParent;
            NElement lastElement = dummyParent;

            bool addToCurrent = false;
            string lastAttributeName = null;

            while (tr.Read())
            {
                switch (tr.Token)
                {
                    case NimlToken.StartTag:
                        lastElement = new NElement(tr.Value);
                        if (addToCurrent)
                        {
                            currentElement.AddChild(lastElement);
                            addToCurrent = false;
                        }
                        else
                        {
                            currentElement = lastElement;
                            parents.Peek().AddChild(currentElement);
                        }
                        break;


                    case NimlToken.InlineText:
                    case NimlToken.MultilineText:
                        lastElement.AddChildText(tr.Value);
                        break;

                    case NimlToken.Text:
                        var text = new NText(tr.Value);
                        if (addToCurrent)
                        {
                            currentElement.AddChild(text);
                            addToCurrent = false;
                        }
                        else
                        {
                            parents.Peek().AddChild(text);
                        }
                        break;

                    case NimlToken.IndentDecrease:
                        if (parents.Count > 1)
                            currentElement = parents.Pop();
                        break;

                    case NimlToken.EnterElement:
                        parents.Push(lastElement);
                        break;

                    case NimlToken.AddToLastElement:
                        addToCurrent = true;
                        break;

                    case NimlToken.AttributeName:
                        lastAttributeName = tr.Value;
                        if (lastAttributeName == String.Empty)
                            break;

                        if (lastElement.Attributes == null)
                            lastElement.Attributes = new Dictionary<string, string>();

                        lastElement.Attributes.Add(lastAttributeName, null);
                        break;

                    case NimlToken.AttributeValue:
                        lastElement.Attributes[lastAttributeName] = tr.Value;
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
