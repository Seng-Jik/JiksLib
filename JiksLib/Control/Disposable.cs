using System;
using System.Collections.Generic;

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
        public static readonly IDisposable Null = FromAction(() => { });

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
        public static IDisposable Merge(IEnumerable<IDisposable> disposables)
        {
            return Scope(f =>
            {
                foreach (var i in disposables) f(i);
            });
        }

        /// <summary>
        /// 合并多个 IDisposable 实例为一个
        /// 执行的时候，将会从右到左执行
        /// </summary>
        /// <param name="disposables">要合并的 IDisposables</param>
        /// <returns>合并后的 IDisposable</returns>
        public static IDisposable Merge(params IDisposable[] disposables) =>
            Merge((IEnumerable<IDisposable>)disposables);

        /// <summary>
        /// 这个委托用于向 Disposable.Scope 提交一个 IDisposable 实例
        /// </summary>
        public delegate void SubmitDisposable(IDisposable disposable);

        /// <summary>
        /// 通过一个作用域创建一个 IDisposable
        /// 作用域会被传入一个 SubmitDisposable 委托
        /// 调用该委托即提交一个 IDisposable
        /// 生成的 IDisposable 被调用时，将会以相反的顺序调用提交的各 IDisposable
        /// </summary>
        /// <param name="scope">作用域</param>
        /// <returns>生成的 IDisposable</returns>
        public static IDisposable Scope(Action<SubmitDisposable> scope)
        {
            Stack<IDisposable> disposableStack = new();
            scope(disposableStack.Push);

            return FromAction(() =>
            {
                while (disposableStack.Count > 0)
                    disposableStack.Pop().Dispose();
            });
        }

        #region 实现细节

        private class ActionDisposable : IDisposable
        {
            readonly Action disposeAction;
            bool disposed = false;

            public ActionDisposable(Action disposeAction)
            {
                this.disposeAction = disposeAction;
            }

            public void Dispose()
            {
                if (disposed)
                    throw new InvalidOperationException(
                        "This IDisposable has already been disposed.");

                disposeAction();
                disposed = true;
            }
        }

        #endregion
    }
}