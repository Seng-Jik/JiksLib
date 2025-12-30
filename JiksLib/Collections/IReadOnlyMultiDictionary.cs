using System.Collections.Generic;

namespace JiksLib.Collections
{
    public interface IReadOnlyMultiDictionary<TKey, TValue> :
        IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>,
        IReadOnlyCollection<KeyValuePair<TKey, IEnumerable<TValue>>>
        where TKey : notnull
        where TValue : notnull
    {
        /// <summary>
        /// 判断字典是否包含某个键
        /// </summary>
        /// <param name="key">要判断的键</param>
        /// <returns>是否包含</returns>
        bool ContainsKey(TKey key);

        /// <summary>
        /// 获取指定键对应的值的数量
        /// </summary>
        int GetValueCountOfKey(TKey key);

        /// <summary>
        /// 所有的键
        /// </summary>
        IEnumerable<TKey> Keys { get; }

        /// <summary>
        /// 所有的值
        /// </summary>
        IEnumerable<TValue> Values { get; }

        /// <summary>
        /// 获取指定键对应的值集合
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>值集合</returns>
        IEnumerable<TValue> this[TKey key] { get; }
    }
}
