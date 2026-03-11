using System;
using System.Collections.Generic;
using System.Linq;
using JiksLib.Control;

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
            /// 检查所有红点
            /// </summary>
            /// <param name="redPointNumberSum">所有红点的值之和</param>
            /// <returns>是否有红点</returns>
            bool Check(out int redPointNumberSum);

            /// <summary>
            /// 是否存在任一红点
            /// </summary>
            bool Check();
        }

        /// <summary>
        /// 红点检查器
        /// </summary>
        public interface IRedPointChecker<TKeyB> : IRedPointChecker
            where TKeyB : notnull
        {
            public delegate void OnRedPointChangedHandler(
                IEnumerable<TKeyB> keyB);

            /// <summary>
            /// 当有一个或多个红点状态发生改变时触发该事件
            /// </summary>
            event OnRedPointChangedHandler? OnRedPointChanged;

            /// <summary>
            /// 检查单个红点
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
        /// 简单的红点检查器，只有零个或一个红点
        /// 该红点器的红点键始终为 (keyA, new UnitType())
        /// </summary>
        /// <param name="keyA">一级红点键</param>
        /// <param name="number">输出的红点的值</param>
        public delegate bool SimpleRedPointChecker(
            TKeyA keyA,
            out int number);

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

        public abstract class RedPointFamily
        {
            /// <summary>
            /// 检查该 RedFamily 中是否存在任一红点，且返回所有红点值的和
            /// </summary>
            public bool CheckFamily(out int number)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// 检查 RedFamily 中是否存在任一红点
            /// </summary>
            public bool CheckFamily()
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// 为该红点家族下所有的红点添加监听器
            /// 只能用 RemoveListenerForFamily 删除
            /// </summary>
            public void AddListenerForFamily<TKeyB>(
                RedPointListener<TKeyB> listener)
                where TKeyB : notnull
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// 为该红点家族下的所有红点删除监听器
            /// 只能用 AddListenerForFamily 添加的监听器
            /// </summary>
            public void RemoveListenerForFamily<TKeyB>(
                RedPointListener<TKeyB> listener)
                where TKeyB : notnull
            {
                throw new NotImplementedException();
            }

            internal readonly TKeyA KeyA;
            internal readonly List<RedPointFamily> Parents = new();
            internal readonly RedPointFamily[] Children;

            internal RedPointFamily(
                TKeyA keyA,
                RedPointFamily[] children)
            {
                KeyA = keyA;
                Children = children;
            }

            internal abstract bool RawCheckFamily(out int number);
            internal abstract bool RawCheckFamily();
        }


        public abstract class RedPointFamily<TKeyB> : RedPointFamily
            where TKeyB : notnull
        {
            /// <summary>
            /// 检查红点是否存在及红点值
            /// </summary>
            public bool Check(
                TKeyB keyB,
                out int number,
                out TUserData userData)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// 检查红点是否存在，不计算红点值
            /// 不需要红点值时使用该函数，性能更好
            /// </summary>
            public bool Check(
                TKeyA keyA,
                TKeyB keyB,
                out TUserData userData)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// 为键为 (keyA, keyB) 的红点添加监听器
            /// 只能用 RemoveListener 删除
            /// </summary>
            public void AddListener(
                TKeyB keyB,
                RedPointListener<TKeyB> listener)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// 为键为 (keyA, keyB) 的红点删除监听器
            /// 只能删除用 AddListener 添加的监听器
            /// </summary>
            public void RemoveListener(
                TKeyB keyB,
                RedPointListener<TKeyB> listener)
            {
                throw new NotImplementedException();
            }

            internal RedPointFamily(
                TKeyA keyA,
                RedPointFamily[] children) :
                base(keyA, children)
            { }

            internal abstract bool RawCheck(
                TKeyB keyB,
                out int number,
                out TUserData userData);
        }

        public abstract class RedPoint : RedPointFamily<UnitType>
        {
            internal RedPoint(
                TKeyA keyA,
                RedPointFamily[] children) : base(keyA, children)
            { }
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
                layout.Add(
                    keyA,
                    (null, new RedPointCheckerWrapper<TKeyB>(keyA, checker)));
            }

            /// <summary>
            /// 添加一个简单红点检查器
            /// KeyB 为 UnitType
            /// </summary>
            public void AddSimple(
                TKeyA keyA,
                SimpleRedPointChecker checker,
                TUserData userData)
            {
                SimpleRedPointCheckerWrapper w = new(keyA, userData, checker);
                layout.Add(keyA, (null, w));
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
                Dictionary<TKeyA, RedPointFamily> result = new();
                Dictionary<TKeyA, RedPointFamily> currentRoundCreated = new();

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

            RedPointFamily BuildRedPointFamily(
                IReadOnlyDictionary<TKeyA, RedPointFamily> alreadyBuilt,
                Dictionary<TKeyA, RedPointFamily> currentRoundCreated,
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

                        RedPointFamily[] children =
                            new RedPointFamily[childrenKeys.Length];

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
                        currentRoundCreated.Add(keyA, value.Item2);
                        return value.Item2;
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

            readonly Dictionary<TKeyA, ((TUserData userData, TKeyA[])?, RedPointFamily)>
                layout = new();

            bool built = false;
        }

        RedPointSolver(IReadOnlyDictionary<TKeyA, RedPointFamily> redNodeGraph)
        {
            graph = redNodeGraph;
        }

        readonly IReadOnlyDictionary<TKeyA, RedPointFamily> graph;


        private sealed class RedPointCheckerWrapper<TKeyB> : RedPointFamily<TKeyB>
            where TKeyB : notnull
        {
            readonly IRedPointChecker<TKeyB> checker;

            internal RedPointCheckerWrapper(
                TKeyA keyA,
                IRedPointChecker<TKeyB> redPointChecker) :
                base(keyA, Array.Empty<RedPointFamily>())
            {
                checker = redPointChecker;
            }

            internal override bool RawCheck(
                TKeyB keyB,
                out int number,
                out TUserData userData)
            {
                if (!checker.Check(keyB, out number, out userData))
                {
                    number = 0;
                    return false;
                }

                return true;
            }

            internal override bool RawCheckFamily() =>
                checker.CheckFamily();

            internal override bool RawCheckFamily(out int number)
            {
                if (!checker.CheckFamily(out number))
                {
                    number = 0;
                    return false;
                }

                return true;
            }
        }

        private sealed class SimpleRedPointCheckerWrapper : RedPoint
        {
            internal readonly SimpleRedPointChecker Checker;
            internal readonly TUserData userData;

            internal SimpleRedPointCheckerWrapper(
                TKeyA keyA,
                TUserData userData,
                SimpleRedPointChecker checker) :
                base(keyA, Array.Empty<RedPointFamily>())
            {
                Checker = checker;
                this.userData = userData;
            }

            internal override bool RawCheck(
                UnitType keyB,
                out int number,
                out TUserData userData)
            {
                userData = this.userData;
                return RawCheckFamily(out number);
            }

            internal override bool RawCheckFamily(out int number)
            {
                if (!Checker(KeyA, out number))
                {
                    number = 0;
                    return false;
                }

                return true;
            }

            internal override bool RawCheckFamily() =>
                RawCheckFamily(out _);
        }

        private sealed class RedPointComposite : RedPoint
        {
            readonly TUserData userData;

            internal RedPointComposite(
                TKeyA keyA,
                TUserData userData,
                RedPointFamily[] children) : base(keyA, children)
            {
                this.userData = userData;
            }

            internal override bool RawCheck(
                UnitType keyB,
                out int number,
                out TUserData userData)
            {
                userData = this.userData;
                return RawCheckFamily(out number);
            }

            internal override bool RawCheckFamily(out int number)
            {
                bool r = false;
                number = 0;

                foreach (var i in Children)
                {
                    if (i.RawCheckFamily(out var n))
                    {
                        r = true;
                        number += n;
                    }
                }

                return r;
            }

            internal override bool RawCheckFamily()
            {
                foreach (var i in Children)
                    if (i.RawCheckFamily())
                        return true;

                return false;
            }
        }
    }
}

