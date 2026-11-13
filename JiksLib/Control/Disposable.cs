using System;
using System.Threading;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 创建 <see cref="IDisposable"/> 对象的辅助类
    /// </summary>
    public static class Disposable
    {
        /// <summary>
        /// 获取一个空的 <see cref="IDisposable"/> 对象
        /// 其 Dispose 方法不做任何事情，可安全地重复调用
        /// 全局共享同一个实例，任何持有者都不应假设自己独占它
        /// </summary>
        public static IDisposable Empty => empty ??= new();

        /// <summary>
        /// 创建一个在 Dispose 时执行指定委托的 <see cref="IDisposable"/> 对象
        /// 该委托最多只会被执行一次，即使 Dispose 被重复调用
        /// </summary>
        /// <param name="disposeAction">Dispose 时要执行的操作，不可为 null</param>
        /// <exception cref="ArgumentNullException">disposeAction 为 null</exception>
        public static IDisposable FromAction(Action disposeAction) =>
            new ActionDisposable(disposeAction.ThrowIfNull(nameof(disposeAction)));

        #region Details

        static EmptyDisposable? empty;

        private sealed class EmptyDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        private sealed class ActionDisposable : IDisposable
        {
            public ActionDisposable(Action disposeAction)
            {
                this.disposeAction = disposeAction;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref disposeAction, null)?.Invoke();
            }

            Action? disposeAction;
        }

        #endregion
    }
}
