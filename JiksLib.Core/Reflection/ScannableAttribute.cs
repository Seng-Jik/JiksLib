using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JiksLib.Extensions;

namespace JiksLib.Reflection
{
    using InternalResult =
        IReadOnlyDictionary<Type, IReadOnlyList<(ScannableAttribute, Type)>>;

    /// <summary>
    /// 可被扫描的Attributes
    /// </summary>
    public abstract class ScannableAttribute : Attribute
    {
        public sealed class ScanResult
        {
            readonly InternalResult result;

            internal ScanResult(InternalResult result)
            {
                this.result = result;
            }

            /// <summary>
            /// 获取指定类型 ScannableAttribute 标记的类型列表
            /// </summary>
            /// <typeparam name="TAttr">ScannableAttribute的子类型</typeparam>
            /// <returns>所有标记了这个ScannableAttribute的类型</returns>
            public IEnumerable<(TAttr Attr, Type Type)> GetTypesByAttribute<TAttr>()
                where TAttr : ScannableAttribute
            {
                if (result.TryGetValue(typeof(TAttr), out var types))
                    return types.Select(t => ((TAttr)t.Item1, t.Item2));

                return Enumerable.Empty<(TAttr, Type)>();
            }
        }

        /// <summary>
        /// 扫描指定程序集中所有的 ScannableAttribute 标记的类型
        /// 如果指定程序集为 null 则扫描所有程序集
        /// </summary>
        public static ScanResult ScanAssemblies(
            IEnumerable<Assembly>? assemblies = null)
        {
            assemblies ??= AppDomain.CurrentDomain.GetAssemblies();

            InternalResult r = assemblies
                .SelectMany(x => x.GetTypes())
                .Select(x => (x, x.GetCustomAttributes<ScannableAttribute>()))
                .SelectMany(x => x.Item2.Select(attr => (attr, x.x)))
                .GroupBy(x => x.attr.GetType())
                .ToDictionary(
                    x => x.Key,
                    x => x.ToArray().AsReadOnly());

            return new(r);
        }
    }
}
