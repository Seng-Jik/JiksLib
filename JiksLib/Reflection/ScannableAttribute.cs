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
    /// 可被扫描的 Attributes
    /// </summary>
    /// <remarks>
    /// 只扫描显式标记（不沿继承链），可标记在类、结构体、接口、枚举与委托上。
    /// </remarks>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface |
        AttributeTargets.Enum | AttributeTargets.Delegate,
        Inherited = false)]
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
            /// 获取指定具体 Attribute 类型标记的类型列表
            /// 按具体类型精确匹配，查询基类不会返回派生属性的结果
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
        /// 扫描指定程序集中所有显式标记了 ScannableAttribute 的类型
        /// 无法加载的类型会被跳过（ReflectionTypeLoadException），不影响其余类型
        /// </summary>
        /// <param name="assemblies">要扫描的程序集</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="assemblies"/> 为 null 时抛出</exception>
        public static ScanResult ScanAssemblies(
            IEnumerable<Assembly> assemblies)
        {
            assemblies.ThrowIfNull(nameof(assemblies));

            InternalResult r = assemblies
                .Where(a => a != null)
                .SelectMany(GetLoadableTypes)
                .Select(t => (Type: t, Attrs: t.GetCustomAttributes<ScannableAttribute>(inherit: false)))
                .SelectMany(x => x.Attrs.Select(attr => (attr, x.Type)))
                .GroupBy(x => x.attr.GetType())
                .ToDictionary(
                    x => x.Key,
                    x => x.ToArray().AsReadOnly());

            return new(r);
        }

        /// <summary>
        /// 获取程序集中可加载的类型；部分类型加载失败时只返回成功的部分
        /// </summary>
        static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null).Select(t => t!);
            }
        }
    }
}
