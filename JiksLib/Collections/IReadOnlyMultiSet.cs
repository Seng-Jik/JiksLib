using System.Collections.Generic;

namespace JiksLib.Collections
{
    /// <summary>
    /// 只读多重集合
    /// </summary>
    /// <remarks>
    /// 该接口是对可变实现的只读视图，不保证可以强转回可写类型。
    /// 调用方不得通过强制转换绕过只读约束，强转属于未定义行为。
    /// </remarks>
    /// <typeparam name="T">元素类型</typeparam>
    public interface IReadOnlyMultiSet<T> :
        IEnumerable<T>,
        IReadOnlyCollection<T>
        where T : notnull
    {
        /// <summary>
        /// 判断集合是否包含某个元素
        /// </summary>
        /// <param name="item">要判断的元素</param>
        /// <returns>是否包含</returns>
        bool Contains(T item);

        /// <summary>
        /// 判断集合中某个元素的数量
        /// </summary>
        /// <param name="item">要判断的元素</param>
        /// <returns>元素数量</returns>
        int GetCountOf(T item);
    }
}
