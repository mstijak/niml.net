﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Niml
{
    public abstract class NObject
    {
        public abstract XNode ToXNode(String xmlNs = null);
        public abstract NimlObjectType ObjectType { get; }
    }

    public enum NimlObjectType
    {
        Element,
        Text
    }

    public class NText : NObject
    {
        public NText(string v)
        {
            Value = v;
        }

        public bool Raw { get; set; }

        public string Value { get; set; }

        public override XNode ToXNode(String xmlNs = null)
        {
            return new XText(Value);
        }

        public override NimlObjectType ObjectType
        {
            get
            {
                return NimlObjectType.Text;
            }
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

        public override NimlObjectType ObjectType
        {
            get
            {
                return NimlObjectType.Element;
            }
        }

        public XElement ToXElement(string xmlNs)
        {
            var ns = xmlNs;

            if (Attributes != null && !Attributes.TryGetValue("xmlns", out ns))
                ns = xmlNs;

            var el = new XElement(ns != null ? XName.Get(Name, ns) : XName.Get(Name));

            if (Attributes != null)
                foreach (var attr in Attributes)
                    el.SetAttributeValue(XName.Get(attr.Key), attr.Value);

            if (Children != null)
                foreach (var item in Children)
                    el.Add(item.ToXNode(ns));

            return el;
        }

        public override XNode ToXNode(string xmlNs)
        {
            return ToXElement(xmlNs);
        }
    }

    public enum NObjectType
    {
        Text,
        Element
    }
}
