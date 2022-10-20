using System;
using System.Xml.Serialization;

namespace XmlDiff.Models
{
    [XmlRoot]
    public class Diff<T>
        where T : class // for some reason this is required for being able to compare A and B
    {
        [XmlAttribute("name")] public string Name { get; set; }
        [XmlElement("old")] public virtual T A { get; set; }
        [XmlElement("new")] public virtual T B { get; set; }
        public virtual bool IsChanged() => (!A?.Equals(B)??true) && (A != null || B != null);
        public virtual ChangeType GetChangeType()
        {
            int removed = A != null ? 1 : 0;
            int added = B != null ? 2 : 0;
            return (ChangeType)(removed | added);
        }
        public override string ToString()
        {
            return $"{Name}\nA={A}\nB={B}";
        }
    }

    [Flags]
    public enum ChangeType
    {
        None=0,
        Removed=1,
        Added=2,
        Changed=3
    }
}
