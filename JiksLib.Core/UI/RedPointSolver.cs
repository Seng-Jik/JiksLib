using System;
using System.Collections.Generic;

namespace JiksLib.UI
{
    /// <summary>
    /// 红点解算器
    /// 红点构成一个有向无环图
    /// </summary>
    /// <typeparam name="TRedPointKey">红点键</typeparam>
    /// <typeparam name="TUserData">红点附带的用户信息类型</typeparam>
    public sealed class RedPointSolver<TRedPointKey, TUserData>
        where TRedPointKey : notnull
    {
        /// <summary>
        /// 红点监听器
        /// </summary>
        /// <param name="redPointKey">红点键</param>
        /// <param name="userData">红点附带的用户信息</param>
        /// <param name="redPointNumber">红点的值</param>
        public delegate void RedPointListener(
            TRedPointKey redPointKey,
            TUserData userData,
            int? redPointNumber);

        /// <summary>
        /// 红点检查器
        /// </summary>
        /// <param name="key">红点键</param>
        /// <param name="userData">红点附带的用户信息</param>
        /// <param name="number">输出的红点的值</param>
        public delegate bool RedPointChecker(
            TRedPointKey key,
            TUserData userData,
            out int number);

        /// <summary>
        /// 检查红点是否存在及红点值
        /// </summary>
        public bool Check(
            TRedPointKey key,
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
            TRedPointKey key,
            out TUserData userData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 添加红点监听器
        /// 当红点变更时触发
        /// </summary>
        public void AddListener(
            TRedPointKey key,
            RedPointListener listener)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 移除红点监听器
        /// </summary>
        public void RemoveListener(
            TRedPointKey key,
            RedPointListener listener)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 检查指定红点的值，并触发红点监听器
        /// </summary>
        public void UpdateRedPoint(TRedPointKey key)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 红点解算器构造器
        /// </summary>
        public sealed class Builder
        {
            /// <summary>
            /// 添加一个红点，附带一个检查器
            /// </summary>
            public void AddRedPoint(
                TRedPointKey key,
                TUserData userData,
                RedPointChecker checker)
            {
                redPointLayout.Add(key, (userData, checker, null!));
            }

            /// <summary>
            /// 添加一个红点，绑定一个或多个子红点
            /// 当一个子红点有值时，该红点有值
            /// 该红点的值为所有子红点值之和
            /// </summary>
            public void AddRedPoint(
                TRedPointKey key,
                TUserData userData,
                params TRedPointKey[] children)
            {
                redPointLayout.Add(key, (userData, null, children));
            }

            /// <summary>
            /// 构建解算器
            /// </summary>
            public RedPointSolver<TRedPointKey, TUserData> Build()
            {
                throw new NotImplementedException();
            }

            readonly Dictionary<TRedPointKey, (TUserData, RedPointChecker?, TRedPointKey[])>
                redPointLayout = new();
        }
    }
}
