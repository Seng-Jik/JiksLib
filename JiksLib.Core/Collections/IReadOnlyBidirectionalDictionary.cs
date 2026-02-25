using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace JiksLib.Collections
{
    /// <summary>
    /// 只读双向字典
    /// </summary>
    public interface IReadOnlyBidirectionalDictionary<TKey, TValue> :
        IReadOnlyDictionary<TKey, TValue>
    {
        /// <summary>
        /// 获取反向只读字典
        /// </summary>
        public IReadOnlyDictionary<TValue, TKey> Reversed { get; }

        /// <summary>
        /// 检查只读双向字典中是否存在指定 Value
        /// </summary>
        bool ContainsValue(TValue value);

        /// <summary>
        /// 尝试根据 Value 获取其 Key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
#if NETCOREAPP
        bool TryGetKeyByValue(TValue value, [MaybeNullWhen(false)] out TKey key);
#else
        bool TryGetKeyByValue(TValue value, out TKey key);
#endif
    }
}
