using System;
using System.Collections.Generic;

namespace JiksLib.UI
{
    /// <summary>
    /// 红点解算器
    /// 红点构成一个有向无环图
    /// 红点有两个键，键A和键B联合起来可以索引到一个红点
    /// 键A表示静态红点的部分，键B表示动态红点的部分
    /// </summary>
    /// <typeparam name="TKeyA">一级红点键</typeparam>
    /// <typeparam name="TUserData">红点附带的用户信息类型</typeparam>
    public sealed class RedPointSolver<TKeyA, TUserData>
        where TKeyA : notnull
    {
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

        public interface IRedPointChecker
        {
            /// <summary>
            /// 检查所有动态红点
            /// </summary>
            /// <param name="redPointNumberSum">所有动态红点的值之和</param>
            /// <returns>是否有动态红点</returns>
            bool CheckAll(out int redPointNumberSum);
        }

        /// <summary>
        /// 红点检查器
        /// </summary>
        public interface IRedPointChecker<TKeyB> : IRedPointChecker
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
        /// 简单的红点检查器
        /// </summary>
        /// <typeparam name="TKeyB">二级红点键</typeparam>
        /// <param name="key">红点键</param>
        /// <param name="userData">红点附带的用户信息</param>
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
        /// 红点解算器构造器
        /// </summary>
        public sealed class Builder
        {
            /// <summary>
            /// 添加一个红点检查器
            /// 通过 KeyA 来索引到该检查器
            /// 通过 KeyB 来索引到该检查器中的红点
            /// </summary>
            public void AddRedPointChecker<TKeyB>(
                TKeyA keyA,
                IRedPointChecker<TKeyB> checker)
                where TKeyB : notnull
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// 添加一个简单红点检查器
            /// KeyB 会被设置为 UnitType
            /// </summary>
            public void AddStaticRedPoint(
                TKeyA key,
                SimpleRedPointChecker checker,
                TUserData userData)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// 添加一个组合红点，TKeyA 在 children 中的所有红点设置为子红点
            /// 当一个子红点有值时，该红点有值
            /// 该红点的值为所有子红点值之和
            /// </summary>
            public void AddRedPointComposite(
                TKeyA key,
                TUserData userData,
                params TKeyA[] children)
            {
                throw new NotImplementedException();
            }

            /// <summary>
            /// 构建红点解算器
            /// </summary>
            public RedPointSolver<TKeyA, TUserData> Build()
            {
                throw new NotImplementedException();
            }
        }
    }
}

