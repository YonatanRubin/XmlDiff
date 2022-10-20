using System;
using System.Collections.Generic;
using System.Linq;

namespace XmlDiff
{
    internal static class LinqExtensions
    {
        /// <summary>
        /// This code is taken from <a href="https://stackoverflow.com/questions/5489987/linq-full-outer-join"> https://stackoverflow.com/questions/5489987/linq-full-outer-join </a>
        /// </summary>
        /// <param name="a">The first Enumerable to join</param>
        /// <param name="b">The second Enumerable to join</param>
        /// <param name="selectKeyA">The key selector for A</param>
        /// <param name="selectKeyB">The key selector for b</param>
        /// <param name="projection">how to join the two lists together (e.g. what object to create)</param>
        /// <param name="cmp">specific comparer</param>
        /// <typeparam name="TA">type of A values</typeparam>
        /// <typeparam name="TB">type of B values</typeparam>
        /// <typeparam name="TKey">the type of the key to join on</typeparam>
        /// <typeparam name="TResult">the type of the result</typeparam>
        /// <returns>A full outer join of A and B.<example>For the groups <br/>["A":1,"B":2]<br/> and <br/>["A":"alef","C":"gimel"]<br/> it will return <br/>
        /// [ <br/>
        ///     ("A"):[1,"alef"] <br/>
        ///     ("B"):[2,null] <br/>
        ///     ("C"):[null,"gimel"] <br/>
        /// ]
        /// </example>
        /// </returns>
        internal static IEnumerable<TResult> FullOuterGroupJoin<TA, TB, TKey, TResult>(
            this IEnumerable<TA> a,
            IEnumerable<TB> b,
            Func<TA, TKey> selectKeyA,
            Func<TB, TKey> selectKeyB,
            Func<IEnumerable<TA>, IEnumerable<TB>, TKey, TResult> projection,
            IEqualityComparer<TKey> cmp = null)
        {
            cmp ??= EqualityComparer<TKey>.Default;
            a ??= Enumerable.Empty<TA>();
            b ??= Enumerable.Empty<TB>();
            var alookup = a.ToLookup(selectKeyA, cmp);
            var blookup = b.ToLookup(selectKeyB, cmp);

            var keys = new HashSet<TKey>(alookup.Select(p => p.Key), cmp);
            keys.UnionWith(blookup.Select(p => p.Key));

            var join = from key in keys
                let xa = alookup[key]
                let xb = blookup[key]
                select projection(xa, xb, key);

            return join;
        }

        internal static IEnumerable<TResult> FullOuterJoin<TA, TB, TKey, TResult>(
            this IEnumerable<TA> a,
            IEnumerable<TB> b,
            Func<TA, TKey> selectKeyA,
            Func<TB, TKey> selectKeyB,
            Func<TA, TB, TKey, TResult> projection,
            TA defaultA = default(TA),
            TB defaultB = default(TB),
            IEqualityComparer<TKey> cmp = null)
        {
            cmp ??= EqualityComparer<TKey>.Default;
            a ??= Enumerable.Empty<TA>();
            b ??= Enumerable.Empty<TB>();
            var alookup = a.ToLookup(selectKeyA, cmp);
            var blookup = b.ToLookup(selectKeyB, cmp);

            var keys = new HashSet<TKey>(alookup.Select(p => p.Key), cmp);
            keys.UnionWith(blookup.Select(p => p.Key));

            var join = from key in keys
                from xa in alookup[key].DefaultIfEmpty(defaultA)
                from xb in blookup[key].DefaultIfEmpty(defaultB)
                select projection(xa, xb, key);

            return join;
        }
        public static Func<T, bool> Not<T>(this Func<T, bool> f)
        {
            return o => !f(o);
        }
    }
}
