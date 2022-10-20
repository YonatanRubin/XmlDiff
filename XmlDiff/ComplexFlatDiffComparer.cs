using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Serilog;
using XmlDiff.Models;

namespace XmlDiff
{
    public class ComplexFlatDiffComparer : FlatDiffComparer
    {
        private readonly bool _useFullPath;
        private readonly IEnumerable<string> _blackListed;
        private readonly IDictionary<string, string> _idPaths;
        private readonly ILogger _logger = Log.Logger.ForContext<ComplexFlatDiffComparer>();
        public ComplexFlatDiffComparer() : this(Enumerable.Empty<string>(), false, new Dictionary<string, string>())
        {
        }
        public ComplexFlatDiffComparer(IEnumerable<string> blackListed, bool useFullPath, IDictionary<string, string> idPaths)
        {
            _useFullPath = useFullPath;
            _idPaths = idPaths;
            _blackListed = blackListed.ToHashSet();
        }
        public override IEnumerable<FlatNodeDiff> Compare(XElement a, XElement b)
        {
            string name = GetNodeDefinitiveName(a, b);

            if (_blackListed.Contains(name))
            {
                return Enumerable.Empty<FlatNodeDiff>();
            }

            var nodeDiffs = base.Compare(a, b);
            if (_idPaths.ContainsKey(name))
            {
                foreach (var nodeDiff in nodeDiffs.Where(a => a.Id == null))
                {
                    nodeDiff.Id = (b ?? a).XPathSelectElement(_idPaths[name]).Value;
                }
            }
            return nodeDiffs;
        }

        protected override FlatNodeDiff CreateNodeDiff(XElement a, XElement b)
        {
            FlatNodeDiff diff = base.CreateNodeDiff(a, b);
            diff.FullPath = string.Join("/", (a ?? b).AncestorsAndSelf().Skip(_useFullPath ? 0 : 1).Reverse().Select(s => s.Name.LocalName));
            return diff;
        }

        protected override (IEnumerable<XElement> aChange, IEnumerable<XElement> bChange) FindDiffBetweenNodes(IEnumerable<XElement> a, IEnumerable<XElement> b)
        {
            var name = GetNodeDefinitiveName(a?.FirstOrDefault(), b?.FirstOrDefault());
            if (_idPaths.ContainsKey(name))
            {
                var xElementComparer = new XNameElementComparer(_idPaths[name]);
                a = a?.OrderBy(element => element.XPathSelectElement(_idPaths[name])?.Value).ToArray();
                b = b?.OrderBy(element => element.XPathSelectElement(_idPaths[name])?.Value).ToArray();
                IEnumerable<XElement> aChange = a?.Where(LinqExtensions.Not<XElement>(element => b.Contains(element, xElementComparer))).ToArray() ?? Enumerable.Empty<XElement>();
                IEnumerable<XElement> bChange = b?.Where(LinqExtensions.Not<XElement>(element => a.Contains(element, xElementComparer))).ToArray() ?? Enumerable.Empty<XElement>();
                return (aChange, bChange);
            }

            return base.FindDiffBetweenNodes(a, b);

            // return (findDiffBetweenNodes.aChange., findDiffBetweenNodes.bChange.OrderBy(element=>element.XPathSelectElement(_idPaths[name])?.Value));
        }
    }
}
