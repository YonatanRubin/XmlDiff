using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Serilog;
using Serilog.Context;
using XmlDiff.Models;

namespace XmlDiff
{
    public class FlatDiffComparer : IXmlComparer
    {
        private readonly ILogger _logger = Log.Logger.ForContext<FlatDiffComparer>();
        private readonly XElementComparer _xElementComparer = new();
        public virtual IEnumerable<FlatNodeDiff> Compare(XElement a, XElement b)
        {
            if (a == null && b == null)
            {
                return Enumerable.Empty<FlatNodeDiff>();
            }
            FlatNodeDiff diff = CreateNodeDiff(a, b);
            using var property = LogContext.PushProperty("XmlDiffLevel", diff.Name);
            diff.Attributes = CompareAttributes(a?.Attributes(), b?.Attributes()).ToList();
            if (a is null or {HasElements: false} && b is null or {HasElements: false}) // both are leaf
            {
                diff = CompareLeaf(a, b, diff);
                return diff.IsChanged() ? new[] {diff} : Enumerable.Empty<FlatNodeDiff>();
            }

            var aChildren = a?.Elements().GroupBy(element => element.Name.LocalName);
            var bChildren = b?.Elements().GroupBy(element => element.Name.LocalName);
            var childrenGroups = aChildren.FullOuterGroupJoin(bChildren, g => g.Key, g => g.Key, (a, b, key) => (a.FirstOrDefault(g => g.Key == key), b.FirstOrDefault(g => g.Key == key)));
            var subnodesDiffs = new List<FlatNodeDiff>();
            foreach (var (aGrouping, bGrouping) in childrenGroups)
            {
                subnodesDiffs.AddRange(CompareSubNodes(aGrouping, bGrouping));
            }
            if (diff.IsChanged()) subnodesDiffs.Add(diff);
            return subnodesDiffs.Where(d => d.IsChanged());
        }
        protected virtual FlatNodeDiff CreateNodeDiff(XElement a, XElement b)
        {
            return new()
            {
                Name = GetNodeDefinitiveName(a, b)
            };
        }
        protected static string GetNodeDefinitiveName(XElement a, XElement b)
        {
            return a?.Name.LocalName ?? b.Name.LocalName;
        }

        private List<FlatNodeDiff> CompareSubNodes(IEnumerable<XElement> a, IEnumerable<XElement> b)
        {
            a ??= Enumerable.Empty<XElement>();
            b ??= Enumerable.Empty<XElement>();
            var changes = new List<FlatNodeDiff>();
            var (aChange, bChange) = FindDiffBetweenNodes(a, b);
            var d = aChange.Count() - bChange.Count();
            if (d < 0) // some were added
            {
                changes.AddRange(bChange.Take(-d).SelectMany(element => Compare(null, element)).ToArray());
                bChange = bChange.Skip(-d);
            }
            if (d > 0) // some were removed
            {
                changes.AddRange(aChange.Take(d).SelectMany(element => Compare(element, null)).ToArray());
                aChange = aChange.Skip(d);
            }
            if (aChange.Count() != bChange.Count())
            {
                _logger.Warning("the amount of changes between the first subnodes and the second subnodes were not the same. A={aCount}[{XmlDiffLevel}], B={bCount}[{XmlDiffLevel}] over {countChanges} changes", aChange.Count(),
                    bChange.Count(), d);
            }
            var changedDiffs = aChange.Zip(bChange, ValueTuple.Create).Select(node => Compare(node.Item1, node.Item2));
            changes.AddRange(changedDiffs.SelectMany(s => s));
            return changes;
        }


        private FlatNodeDiff CompareLeaf(XElement a, XElement b, FlatNodeDiff diff)
        {
            if (a?.Value == b?.Value)
            {
                return diff;
            }
            if (!(a?.IsEmpty ?? true))
            {
                diff.A = a.Value;
            }
            if (!(b?.IsEmpty ?? true))
            {
                diff.B = b.Value;
            }
            return diff;
        }

        private IEnumerable<Diff<string>> CompareAttributes(IEnumerable<XAttribute> a, IEnumerable<XAttribute> b)
        {
            a ??= Enumerable.Empty<XAttribute>();
            b ??= Enumerable.Empty<XAttribute>();
            var aDict = a.ToDictionary(a => a.Name, a => a.Value);
            var bDict = b.ToDictionary(b => b.Name, b => b.Value);
            var removed = aDict.Keys.Where(key => !bDict.ContainsKey(key)).Select(attribute => new Diff<string>() {Name = attribute.LocalName, A = aDict[attribute.LocalName]});
            var added = bDict.Keys.Where(key => !aDict.ContainsKey(key)).Select(attribute => new Diff<string>() {Name = attribute.LocalName, B = bDict[attribute.LocalName]});
            var changed = aDict.Keys.Intersect(bDict.Keys).Select(attribute =>
                {
                    if (aDict[attribute] == bDict[attribute]) // attribute not changed
                        return null;
                    return new Diff<string>() {Name = attribute.LocalName, A = aDict[attribute], B = bDict[attribute]};
                })
                .Where(diff => diff != null);
            return removed.Concat(added).Concat(changed);
        }

        protected virtual (IEnumerable<XElement> aChange, IEnumerable<XElement> bChange) FindDiffBetweenNodes(IEnumerable<XElement> a, IEnumerable<XElement> b)
        {
            IEnumerable<XElement> aChange = a.Where(LinqExtensions.Not<XElement>(element => b.Contains(element, _xElementComparer))).ToArray() ?? Enumerable.Empty<XElement>();
            // IEnumerable<XElement> aChange = a.Where(LinqExtensions.Not<XElement>(b.Contains)).ToArray() ?? Enumerable.Empty<XElement>();
            IEnumerable<XElement> bChange = b.Where(LinqExtensions.Not<XElement>(element => a.Contains(element, _xElementComparer))).ToArray() ?? Enumerable.Empty<XElement>();
            // IEnumerable<XElement> bChange = b.Where(LinqExtensions.Not<XElement>(a.Contains)).ToArray() ?? Enumerable.Empty<XElement>();
            return (aChange, bChange);
        }
    }

}
