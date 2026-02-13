#nullable enable

using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JiksLib.Extensions;
using static JiksLib.Control.Disposable;

namespace JiksLib.Control.UniTask
{
    /// <summary>
    /// IDisposable 辅助类
    /// </summary>
    public static class Disposable
    {
        /// <summary>
        /// 通过一个异步作用域函数创建一个 IDisposable 实例
        /// 异步作用域将会被传入一个提交 IDisposable 的函数
        /// 适用于异步作用域，不可将 SubmitDisposable 存储到作用域外
        /// </summary>
        /// <typeparam name="R">作用域返回值类型</typeparam>
        /// <param name="scope">作用域</param>
        /// <returns>返回值和 IDisposable</returns>
        public static async UniTask<(R Result, IDisposable Disposable)> ScopeAsync<R>(
            Func<SubmitDisposable, UniTask<R>> scope)
        {
            Stack<IDisposable> disposableStack = new();
            bool scopeEnded = false;

            void dispose()
            {
                List<Exception>? exceptions = null;

                while (disposableStack.Count > 0)
                {
                    try
                    {
                        disposableStack.Pop().Dispose();
                    }
                    catch (Exception e)
                    {
                        exceptions ??= new();
                        exceptions.Add(e);
                    }
                }

                if (exceptions != null)
                {
                    throw new AggregateException(
                        "One or more exceptions occurred while disposing.",
                        exceptions);
                }
            }

            var d = FromAction(dispose);

            try
            {
                var result = await scope(x =>
                {

                    if (scopeEnded)
                        throw new InvalidOperationException(
                            "Cannot submit IDisposable after scope function has already returned.");

                    disposableStack.Push(x.ThrowIfNull());
                });

                scopeEnded = true;

                return (result, d);
            }
            catch
            {
                d.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 通过一个异步作用域函数创建一个 IDisposable 实例
        /// 异步作用域将会被传入一个提交 IDisposable 的函数
        /// 适用于异步作用域，不可将 SubmitDisposable 存储到作用域外
        /// </summary>
        /// <typeparam name="R">作用域返回值类型</typeparam>
        /// <param name="scope">作用域</param>
        /// <returns>返回值和 IDisposable</returns>
        public static async UniTask<IDisposable> ScopeAsync(
            Func<SubmitDisposable, Cysharp.Threading.Tasks.UniTask> scope) =>
            (await ScopeAsync<UnitType>(async f => { await scope(f); return new(); })).Disposable;
    }
}