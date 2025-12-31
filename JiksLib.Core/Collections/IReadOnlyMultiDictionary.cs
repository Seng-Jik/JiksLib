using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Collections
{
    /// <summary>
    /// 只读多重字典
    /// </summary>
    /// <typeparam name="TKey">键的类型</typeparam>
    /// <typeparam name="TValue">值的类型</typeparam>
    public interface IReadOnlyMultiDictionary<TKey, TValue> :
        IEnumerable<KeyValuePair<TKey, TValue>>,
        IReadOnlyCollection<KeyValuePair<TKey, TValue>>,
        ILookup<TKey, TValue>
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
        /// 判断字典中是否包含某个值
        /// </summary>
        /// <param name="value">要判断的值</param>
        /// <returns>是否包含</returns>
        bool ContainsValue(TValue value);

        /// <summary>
        /// 多重字典中是否包含对应的键值对
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns>是否包含该键值对</returns>
        bool Contains(TKey key, TValue value);

        /// <summary>
        /// 多重字典中是否包含对应的键值对
        /// </summary>
        /// <param name="item">键值对</param>
        /// <returns>是否包含该键值对</returns>
        bool Contains(KeyValuePair<TKey, TValue> item);

        /// <summary>
        /// 获取指定键对应的值的数量
        /// </summary>
        int GetValueCountOfKey(TKey key);

        /// <summary>
        /// 获得指定键值对的数量
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns>指定键值对的数量</returns>
        int GetCountOfKeyValuePair(TKey key, TValue value);

        /// <summary>
        /// 获得指定键值对的数量
        /// </summary>
        /// <param name="item">键值对</param>
        /// <returns>指定键值对的数量</returns>
        int GetCountOfKeyValuePair(KeyValuePair<TKey, TValue> item);

        /// <summary>
        /// 所有的键
        /// </summary>
        IEnumerable<TKey> Keys { get; }

        /// <summary>
        /// 所有的值
        /// </summary>
        IEnumerable<TValue> Values { get; }
    }
}
