using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace XmlDiff.Models
{
    public class FlatNodeDiff : Diff<string>,IEquatable<FlatNodeDiff>
    {
        // this is simpler than cleaning the attributes
        [XmlElement("Attributes")] public List<Diff<string>> Attributes { get; set; } = new();
        [XmlAttribute] public string FullPath { get; set; }
        [XmlAttribute] public string Id { get; set; }
        public override bool IsChanged() => base.IsChanged() || Attributes.Any();
        public bool Equals(FlatNodeDiff other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Attributes, other.Attributes) && FullPath == other.FullPath && Id == other.Id;
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FlatNodeDiff)obj);
        }
        public override int GetHashCode()
        {
            return HashCode.Combine(Attributes, FullPath, Id);
        }
    }
}
