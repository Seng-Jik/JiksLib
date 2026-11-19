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
    /// </remarks>
    public static class AsReadOnlyExtension
    {
        public static IReadOnlyDictionary<T, U> AsReadOnly<T, U>(this Dictionary<T, U>? d) where T : notnull => d.ThrowIfNull();

        /// <summary>
        /// 把列表转为只读视图
        /// 注意：实例调用 <c>list.AsReadOnly()</c> 会优先绑定到 <see cref="List{T}"/> 自带的
        /// 实例方法；本重载只在显式静态调用时生效。
        /// </summary>
        public static IReadOnlyList<T> AsReadOnly<T>(this List<T>? ls) => ls.ThrowIfNull();
        public static IReadOnlyList<T> AsReadOnly<T>(this T[]? ls) => ls.ThrowIfNull();
        public static IReadOnlyCollection<T> AsReadOnly<T>(this HashSet<T>? s) => s.ThrowIfNull();
        public static IReadOnlyCollection<T> AsReadOnly<T>(this LinkedList<T>? s) => s.ThrowIfNull();
    }
}
