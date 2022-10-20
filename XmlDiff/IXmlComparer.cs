using System.Collections.Generic;
using System.Xml.Linq;
using XmlDiff.Models;

namespace XmlDiff
{
    public interface IXmlComparer
    {
        //TODO: try to allow non flat node diff
        public IEnumerable<FlatNodeDiff> Compare(XElement a, XElement b);
    }
}
