using System;
using System.Collections.Generic;

namespace JiksLib.Extensions
{
    /// <summary>
    /// 转换各种集合为只读的
    /// </summary>
    /// <remarks>
    /// 这些扩展方法面向本身没有 <c>AsReadOnly</c> 实例方法的集合（数组、<see cref="HashSet{T}"/>、
    /// <see cref="LinkedList{T}"/> 等），用于在旧运行时上补齐只读视图。注意
    /// <see cref="List{T}"/> 自带实例方法 <c>AsReadOnly()</c>，实例调用语法永远优先绑定到
    /// 实例方法，因此这里的 <see cref="List{T}"/> 重载只能通过静态调用
    /// （<c>AsReadOnlyExtension.AsReadOnly(list)</c>）触达；这是 C# 的成员查找规则，不是缺陷。
    /// <para>
    /// 所有重载都返回<b>原集合本身</b>（不复制、不创建只读包装器），只读性仅由返回的接口类型保证；
    /// 调用方不要把它强转回可变类型后修改，否则会绕过只读语义。
    /// </para>
    /// </remarks>
    public static class AsReadOnlyExtension
    {
        /// <summary>
        /// 把字典转为只读字典视图；返回原实例，不复制
        /// </summary>
        /// <exception cref="ArgumentNullException">当 <paramref name="d"/> 为 null 时抛出</exception>
        public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(
            this Dictionary<TKey, TValue>? d)
            where TKey : notnull =>
            d.ThrowIfNull(nameof(d));

        /// <summary>
        /// 把列表转为只读视图
        /// 注意：实例调用 <c>list.AsReadOnly()</c> 会优先绑定到 <see cref="List{T}"/> 自带的
        /// 实例方法；本重载只在显式静态调用时生效。
        /// </summary>
        /// <exception cref="ArgumentNullException">当 <paramref name="ls"/> 为 null 时抛出</exception>
        public static IReadOnlyList<T> AsReadOnly<T>(this List<T>? ls) => ls.ThrowIfNull(nameof(ls));

        /// <summary>
        /// 把数组转为只读列表视图；返回原数组，不复制
        /// </summary>
        /// <exception cref="ArgumentNullException">当 <paramref name="ls"/> 为 null 时抛出</exception>
        public static IReadOnlyList<T> AsReadOnly<T>(this T[]? ls) => ls.ThrowIfNull(nameof(ls));

        /// <summary>
        /// 把哈希集合转为只读集合视图；返回原实例，不复制
        /// </summary>
        /// <exception cref="ArgumentNullException">当 <paramref name="s"/> 为 null 时抛出</exception>
        public static IReadOnlyCollection<T> AsReadOnly<T>(this HashSet<T>? s) => s.ThrowIfNull(nameof(s));

        /// <summary>
        /// 把链表转为只读集合视图；返回原实例，不复制
        /// </summary>
        /// <exception cref="ArgumentNullException">当 <paramref name="s"/> 为 null 时抛出</exception>
        public static IReadOnlyCollection<T> AsReadOnly<T>(this LinkedList<T>? s) => s.ThrowIfNull(nameof(s));
    }
}
