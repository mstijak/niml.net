using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Niml
{
    public abstract class NObject
    {
        public abstract XNode ToXNode();
    }

    public class NText : NObject
    {
        public NText(string v)
        {
            Value = v;
        }

        public bool Raw { get; set; }

        public string Value { get; set; }

        public override XNode ToXNode()
        {
            return new XText(Value);
        }
    }

    public class NElement : NObject
    {
        public string Name { get; set; }

        public NElement(string name)
        {
            this.Name = name;
        }

        public Dictionary<String, string> Attributes { get; set; }

        public List<NObject> Children { get; set; }

        internal void AddChild(NObject child)
        {
            if (Children == null)
                Children = new List<NObject>();

            Children.Add(child);
        }

        internal void AddChildText(string value, bool raw = false)
        {
            if (!raw && String.IsNullOrWhiteSpace(value))
                return;

            AddChild(new NText(value) { Raw = raw });
        }

        public XElement ToXElement()
        {
            var el = new XElement(XName.Get(Name));

            if (Attributes != null)
                foreach (var attr in Attributes)
                    el.SetAttributeValue(XName.Get(attr.Key), attr.Value);

            if (Children != null)
                foreach (var item in Children)
                    el.Add(item.ToXNode());

            return el;
        }

        public override XNode ToXNode()
        {
            return ToXElement();
        }
    }

    public enum NObjectType
    {
        Text, 
        Element        
    }
}
