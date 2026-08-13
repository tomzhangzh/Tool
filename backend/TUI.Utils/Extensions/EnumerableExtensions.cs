
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TUI.Utils.Extensions
{
    /// <summary>
    /// 扩展方法类
    /// </summary>
    public static class EnumerableExtensions
    {
        /// <summary>
        /// 将嵌套的集合展开成一维集合
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <typeparam name="R">集合元素的子集合类型</typeparam>
        /// <param name="source">源集合</param>
        /// <param name="recursion">获取子集合的方法</param>
        /// <returns>展开后的一维集合</returns>
        public static IEnumerable<T> Flatten<T, R>(this T source, Func<T, R> recursion) where R : IEnumerable<T>
        {
            var children = recursion(source);
            foreach (var item in children)
            {
                foreach (var i in Flatten(item, recursion))
                {
                    yield return i;
                }
                yield return item;
            }
        }

        /// <summary>
        /// 将集合按指定大小分组
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="items">源集合</param>
        /// <param name="MaximumNumber">每组最大元素个数</param>
        /// <returns>分组后的集合</returns>
        public static IEnumerable<List<T>> GroupByMaximumNumber<T>(this IEnumerable<T> items, int MaximumNumber)
        {
            if (MaximumNumber <= 0)
            {
                throw new ArgumentException("Chunk size must be positive.", "chunkSize");
            }

            return
                items.Select((item, index) => new { item, index })
                     .GroupBy(pair => pair.index / MaximumNumber, pair => pair.item)
                     .Select(grp => grp.ToList());
        }

        /// <summary>
        /// 将集合按指定大小分块
        /// </summary>
        /// <typeparam name="T">集合元素类型</typeparam>
        /// <param name="enumerable">源集合</param>
        /// <param name="chunkSize">每块最大元素个数</param>
        /// <returns>分块后的集合</returns>
        public static IEnumerable<IEnumerable<T>> Chunks<T>(this IEnumerable<T> enumerable,
                                                    int chunkSize)
        {
            if (chunkSize < 1) throw new ArgumentException("chunkSize must be positive");

            using (var e = enumerable.GetEnumerator())
                while (e.MoveNext())
                {
                    var remaining = chunkSize;    // elements remaining in the current chunk
                    var innerMoveNext = new Func<bool>(() => --remaining > 0 && e.MoveNext());

                    yield return e.GetChunk(innerMoveNext);
                    while (innerMoveNext()) {/* discard elements skipped by inner iterator */}
                }
        }

        private static IEnumerable<T> GetChunk<T>(this IEnumerator<T> e,
                                                  Func<bool> innerMoveNext)
        {
            do yield return e.Current;
            while (innerMoveNext());
        }
    }
}


