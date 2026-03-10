using System;
using System.Collections.Generic;
using System.Linq;
using JiksLib.Control;
using JiksLib.Extensions;

namespace JiksLib.UI
{
    /// <summary>
    /// 红点解算器
    /// 红点构成一个有向无环图
    /// 红点有两个键，键A和键B联合起来可以索引到一个红点
    /// 键A与检查器绑定，键B由检查器解释
    /// </summary>
    /// <typeparam name="TKeyA">一级红点键</typeparam>
    /// <typeparam name="TUserData">红点附带的用户信息类型</typeparam>
    public sealed class RedPointSolver<TKeyA, TUserData>
        where TKeyA : notnull
    {
        /// <summary>
        /// 红点检查器
        /// 不要直接使用该类型，要使用 IRedPointChecker<TKeyB>
        /// </summary>
        public interface IRedPointChecker
        {
            /// <summary>
            /// 检查所有动态红点
            /// </summary>
            /// <param name="redPointNumberSum">所有动态红点的值之和</param>
            /// <returns>是否有动态红点</returns>
            bool CheckFamily(out int redPointNumberSum);

            /// <summary>
            /// 是否存在任一红点
            /// </summary>
            bool CheckFamily();
        }

        /// <summary>
        /// 红点检查器
        /// </summary>
        public interface IRedPointChecker<in TKeyB> : IRedPointChecker
            where TKeyB : notnull
        {
            /// <summary>
            /// 检查单个动态红点
            /// </summary>
            /// <param name="keyB">二级红点键</param>
            /// <param name="redPointNumber">红点值</param>
            /// <param name="userData">红点附带的用户信息</param>
            /// <returns>该红点是否存在</returns>
            bool Check(
                TKeyB keyB,
                out int redPointNumber,
                out TUserData userData);
        }

        /// <summary>
        /// 简单的红点检查器，只绑定一个红点
        /// 该检查器的二级红点键始终为 new UnitType()
        /// </summary>
        /// <param name="keyA">一级红点键</param>
        /// <param name="number">输出的红点的值</param>
        public delegate bool SimpleRedPointChecker(
            TKeyA keyA,
            out int number);

        /// <summary>
        /// 检查红点是否存在及红点值
        /// </summary>
        public bool Check<TKeyB>(
            TKeyA keyA,
            TKeyB keyB,
            out int number,
            out TUserData userData)
            where TKeyB : notnull
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 检查红点是否存在，不计算红点值
        /// 不需要红点值时使用该函数，性能更好
        /// </summary>
        public bool Check<TKeyB>(
            TKeyA keyA,
            TKeyB keyB,
            out TUserData userData)
            where TKeyB : notnull
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 检查 KeyA 为某一值的所有红点
        /// </summary>
        public bool CheckFamily(TKeyA keyA, out int number)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 检查是否存在 KeyA 为某一值的红点
        /// </summary>
        public bool CheckFamily(TKeyA keyA)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 红点监听器
        /// </summary>
        /// <typeparam name="TKeyB">二级红点键</typeparam>
        public delegate void RedPointListener<TKeyB>(
            TKeyA keyA,
            TKeyB keyB,
            TUserData userData,
            int? redPointNumber)
            where TKeyB : notnull;

        /// <summary>
        /// 为键为 (keyA, keyB) 的红点添加监听器
        /// 只能用 RemoveListener 删除
        /// </summary>
        public void AddListener<TKeyB>(
            TKeyA keyA,
            TKeyB keyB,
            RedPointListener<TKeyB> listener)
            where TKeyB : notnull
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 为键为 (keyA, keyB) 的红点删除监听器
        /// 只能删除用 AddListener 添加的监听器
        /// </summary>
        public void RemoveListener<TKeyB>(
            TKeyA keyA,
            TKeyB keyB,
            RedPointListener<TKeyB> listener)
            where TKeyB : notnull
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 所有一级红点键为 keyA 的红点添加监听器
        /// 只能用 RemoveListenerForFamily 删除
        /// </summary>
        public void AddListenerForFamily<TKeyB>(
            TKeyA keyA,
            RedPointListener<TKeyB> listener)
            where TKeyB : notnull
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 为所有一级红点键为 keyA 的红点删除监听器
        /// 只能用 AddListenerForFamily 添加的监听器
        /// </summary>
        public void RemoveListenerForFamily<TKeyB>(
            TKeyA keyA,
            RedPointListener<TKeyB> listener)
            where TKeyB : notnull
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 红点解算器构造器
        /// </summary>
        public sealed class Builder
        {
            /// <summary>
            /// 添加一个红点检查器
            /// 通过 KeyA 来索引到该检查器
            /// 通过 KeyB 来索引到该检查器中的红点
            /// </summary>
            public void AddFamily<TKeyB>(
                TKeyA keyA,
                IRedPointChecker<TKeyB> checker)
                where TKeyB : notnull
            {
                layout.Add(keyA, (null, checker));
            }

            /// <summary>
            /// 添加一个简单红点检查器
            /// KeyB 会被设置为 UnitType
            /// </summary>
            public void AddSimple(
                TKeyA keyA,
                SimpleRedPointChecker checker,
                TUserData userData)
            {
                AddFamily(
                    keyA,
                    new SimpleRedPointCheckerWrapper(
                        keyA,
                        userData,
                        checker));
            }

            /// <summary>
            /// 添加一个组合红点，组合红点的值为其子检查器下所有红点（包括所有KeyB）的值之和
            /// 当一个子红点有值时，该红点有值
            /// </summary>
            public void AddComposite(
                TKeyA keyA,
                TUserData userData,
                params TKeyA[] children)
            {
                layout.Add(keyA, ((userData, children), null!));
            }

            /// <summary>
            /// 构建红点解算器
            /// </summary>
            public RedPointSolver<TKeyA, TUserData> Build()
            {
                if (built)
                    throw new InvalidOperationException(
                        "Build() is already called or broken, create a new builder and retry.");

                built = true;
                Dictionary<TKeyA, RedPointNode> result = new();
                Dictionary<TKeyA, RedPointNode> currentRoundCreated = new();

                while (layout.Count > 0)
                {
                    var firstKey = layout.Keys.First();
                    BuildRedPointFamily(result, currentRoundCreated, firstKey);

                    foreach (var i in currentRoundCreated)
                        result.Add(i.Key, i.Value);

                    currentRoundCreated.Clear();
                }

                foreach (var i in result.Values)
                    foreach (var child in i.Children)
                        child.Parents.Add(i);

                foreach (var i in result.Values)
                    i.Parents.TrimExcess();

                return new(result);
            }

            RedPointNode BuildRedPointFamily(
                IReadOnlyDictionary<TKeyA, RedPointNode> alreadyBuilt,
                Dictionary<TKeyA, RedPointNode> currentRoundCreated,
                TKeyA keyA)
            {
                if (alreadyBuilt.TryGetValue(keyA, out var redPoint))
                    return redPoint;

                if (layout.TryGetValue(keyA, out var value))
                {
                    layout.Remove(keyA);

                    if (value.Item1 != null)
                    {
                        var childrenKeys = value.Item1.Value.Item2;

                        RedPointNode[] children =
                            new RedPointNode[childrenKeys.Length];

                        for (int i = 0; i < childrenKeys.Length; ++i)
                            children[i] =
                                BuildRedPointFamily(
                                    alreadyBuilt,
                                    currentRoundCreated,
                                    childrenKeys[i]);

                        RedPointComposite c = new(
                            keyA,
                            value.Item1.Value.userData,
                            children);

                        currentRoundCreated.Add(keyA, c);
                        return c;
                    }
                    else
                    {
                        RedPointLeaf f = new(keyA, value.Item2);
                        currentRoundCreated.Add(keyA, f);
                        return f;
                    }
                }

                if (currentRoundCreated.ContainsKey(keyA))
                {
                    throw new InvalidOperationException(
                        $"KeyA {keyA} is in a cycle, or has circle in red point dependency.");
                }

                throw new InvalidOperationException(
                    $"KeyA {keyA} not found in layout.");
            }

            readonly Dictionary<TKeyA, ((TUserData userData, TKeyA[])?, IRedPointChecker)>
                layout = new();

            bool built = false;

            private sealed class SimpleRedPointCheckerWrapper :
                IRedPointChecker<UnitType>
            {
                readonly TKeyA keyA;
                readonly TUserData userData;
                readonly SimpleRedPointChecker checker;

                internal SimpleRedPointCheckerWrapper(
                    TKeyA keyA,
                    TUserData userData,
                    SimpleRedPointChecker checker)
                {
                    this.keyA = keyA;
                    this.userData = userData;
                    this.checker = checker.ThrowIfNull();
                }

                public bool Check(
                    UnitType _,
                    out int redPointNumber,
                    out TUserData userData)
                {
                    userData = this.userData;

                    if (!checker(keyA, out redPointNumber))
                    {
                        redPointNumber = 0;
                        return false;
                    }

                    return true;
                }

                public bool CheckFamily(out int redPointNumberSum) =>
                    checker(keyA, out redPointNumberSum);

                public bool CheckFamily() => checker(keyA, out _);
            }
        }

        RedPointSolver(IReadOnlyDictionary<TKeyA, RedPointNode> redNodeGraph)
        {
            graph = redNodeGraph;
        }

        readonly IReadOnlyDictionary<TKeyA, RedPointNode> graph;

        private abstract class RedPointNode
        {
            internal readonly TKeyA KeyA;
            internal readonly List<RedPointNode> Parents = new();
            internal readonly RedPointNode[] Children;

            internal RedPointNode(
                TKeyA keyA,
                RedPointNode[] children)
            {
                KeyA = keyA;
                Children = children;
            }
        }

        private sealed class RedPointLeaf : RedPointNode
        {
            internal readonly IRedPointChecker Checker;

            internal RedPointLeaf(TKeyA keyA, IRedPointChecker redPointChecker) :
                base(keyA, Array.Empty<RedPointNode>())
            {
                Checker = redPointChecker;
            }
        }

        private sealed class RedPointComposite : RedPointNode
        {
            readonly TUserData userData;

            internal RedPointComposite(
                TKeyA keyA,
                TUserData userData,
                RedPointNode[] children) : base(keyA, children)
            {
                this.userData = userData;
            }
        }
    }
}
