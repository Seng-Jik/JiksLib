using System;
using System.Collections.Generic;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// IDisposable 辅助类
    /// </summary>
    public static class Disposable
    {
        /// <summary>
        /// 空的 IDisposable 实例
        /// </summary>
        public static readonly IDisposable Null = FromAction(static () => { });

        /// <summary>
        /// 从 Action 创建 IDisposable 实例
        /// </summary>
        /// <param name="disposeAction">要执行的释放资源函数</param>
        /// <returns>IDisposable 实例</returns>
        public static IDisposable FromAction(Action disposeAction) =>
            new ActionDisposable(disposeAction);

        /// <summary>
        /// 合并多个 IDisposable 实例为一个
        /// 执行的时候，将会以相反的顺序执行它们
        /// </summary>
        /// <param name="disposables">要合并的 IDisposables</param>
        /// <returns>合并后的 IDisposable</returns>
        public static IDisposable Merge(IEnumerable<IDisposable> disposables) =>
            Scope(f => { foreach (var i in disposables) f(i); });

        /// <summary>
        /// 合并多个 IDisposable 实例为一个
        /// 执行的时候，将会从右到左执行
        /// </summary>
        /// <param name="disposables">要合并的 IDisposables</param>
        /// <returns>合并后的 IDisposable</returns>
        public static IDisposable Merge(params IDisposable[] disposables) =>
            Scope(f => { for (int i = 0; i < disposables.Length; ++i) f(disposables[i]); });

        /// <summary>
        /// 这个委托用于向 Disposable.Scope 提交一个 IDisposable 实例
        /// </summary>
        public delegate void SubmitDisposable(IDisposable disposable);

        /// <summary>
        /// 通过一个作用域函数创建一个 IDisposable 实例
        /// 作用域将会被传入一个提交 IDisposable 的函数
        /// 不适用于异步作用域，也不可将 SubmitDisposable 存储到作用域外
        /// </summary>
        /// <typeparam name="R">作用域返回值类型</typeparam>
        /// <param name="scope">作用域</param>
        /// <returns>返回值和 IDisposable</returns>
        public static (R Result, IDisposable Disposable) Scope<R>(
            Func<SubmitDisposable, R> scope)
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
                var result = scope(x =>
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
        /// 通过一个作用域函数创建一个 IDisposable 实例
        /// 作用域将会被传入一个提交 IDisposable 的函数
        /// 不适用于异步作用域，也不可将 SubmitDisposable 存储到作用域外
        /// </summary>
        /// <param name="scope">作用域</param>
        /// <returns>IDisposable 实例</returns>
        public static IDisposable Scope(Action<SubmitDisposable> scope) =>
            Scope<UnitType>(f => { scope(f); return new(); }).Disposable;

        private class ActionDisposable : IDisposable
        {
            internal bool Disposed { get; private set; } = false;

            readonly Action disposeAction;

            public ActionDisposable(Action disposeAction)
            {
                this.disposeAction = disposeAction.ThrowIfNull();
            }

            public void Dispose()
            {
                if (Disposed) return;
                disposeAction();
                Disposed = true;
            }
        }
    }
}
