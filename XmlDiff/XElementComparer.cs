using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;

namespace XmlDiff
{
    internal class XElementComparer : IEqualityComparer<XElement>
    {
        public bool Equals(XElement x, XElement y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return x.HasAttributes == y.HasAttributes && Equals(x.FirstAttribute?.Name, y.FirstAttribute?.Name) && Equals(x.FirstAttribute?.Value, y.FirstAttribute?.Value) && x.HasElements == y.HasElements &&
                   x.IsEmpty == y.IsEmpty && Equals(x.LastAttribute?.Name, y.LastAttribute?.Name) && Equals(x.LastAttribute?.Value, y.LastAttribute?.Value) && x.Name.Equals(y.Name) && x.NodeType == y.NodeType && x.Value == y.Value;
        }
        public int GetHashCode(XElement obj)
        {
            return HashCode.Combine(obj.FirstAttribute, obj.HasAttributes, obj.HasElements, obj.IsEmpty, obj.LastAttribute, obj.Name, (int)obj.NodeType, obj.Value);
        }
    }

    internal class XNameElementComparer : IEqualityComparer<XElement>
    {
        private string _identifierPath;
        public XNameElementComparer(string identifierPath)
        {
            _identifierPath = identifierPath;
        }
        public bool Equals(XElement x, XElement y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            if (x.XPathSelectElement(_identifierPath)?.Value != y.XPathSelectElement(_identifierPath)?.Value) return false;
            return Equals(x.FirstAttribute, y.FirstAttribute) && x.HasAttributes == y.HasAttributes && x.HasElements == y.HasElements && x.IsEmpty == y.IsEmpty && Equals(x.LastAttribute, y.LastAttribute) && x.Name.Equals(y.Name) &&
                   x.NodeType == y.NodeType && x.Value == y.Value;
        }
        public int GetHashCode(XElement obj)
        {
            return HashCode.Combine(obj.FirstAttribute, obj.HasAttributes, obj.HasElements, obj.IsEmpty, obj.LastAttribute, obj.Name, (int)obj.NodeType, obj.Value);
        }
    }
}
