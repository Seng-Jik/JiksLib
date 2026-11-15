using System;
using System.Collections.Generic;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 集中管理一组待释放对象的栈
    /// 调用 Dispose 时会按照添加顺序的逆序（后进先出）依次释放其中的所有对象
    /// 即使其中某个对象在释放时抛出异常，剩余对象仍会被释放，全部释放完成后才重新抛出异常
    /// Dispose 可安全地重复调用，栈为空时不会做任何事情
    /// Dispose 后 DisposableStack 会被清空，但仍可继续作为空栈使用，之后添加的对象会在下一次 Dispose 时释放
    /// 此类不是线程安全的，只应在同一个线程上使用
    /// </summary>
    public sealed class DisposableStack : IDisposable
    {
        /// <summary>
        /// 创建一个空的 DisposableStack
        /// </summary>
        public DisposableStack()
        {
            stack = new();
        }

        /// <summary>
        /// 创建一个空的 DisposableStack，并指定初始容量
        /// </summary>
        /// <param name="capacity">初始容量，用于减少添加对象时的扩容次数</param>
        /// <exception cref="ArgumentOutOfRangeException">capacity 小于 0</exception>
        public DisposableStack(int capacity)
        {
            stack = new(capacity);
        }

        /// <summary>
        /// 添加一个需要释放的对象
        /// 释放时会在其 Dispose 被调用后从栈中移除
        /// </summary>
        /// <param name="disposable">需要释放的对象，不可为 null</param>
        /// <exception cref="ArgumentNullException">disposable 为 null</exception>
        public void Add(IDisposable disposable)
        {
            stack.Push(new(disposable.ThrowIfNull(nameof(disposable))));
        }

        /// <summary>
        /// 添加一个在释放时应执行的委托
        /// 释放时会在该委托执行后将其从栈中移除
        /// </summary>
        /// <param name="disposeAction">需要执行的委托，不可为 null</param>
        /// <exception cref="ArgumentNullException">disposeAction 为 null</exception>
        public void Add(Action disposeAction)
        {
            stack.Push(new(disposeAction.ThrowIfNull(nameof(disposeAction))));
        }

        /// <summary>
        /// 返回自身，便于把 DisposableStack 交给只接受 <see cref="IDisposable"/> 的 API 或 using 语句
        /// 释放返回的对象等同于释放 DisposableStack 自身
        /// </summary>
        public IDisposable AsDisposable() => this;

        /// <summary>
        /// 按照添加顺序的逆序依次释放所有对象
        /// 若某个对象释放时抛出异常，其余对象仍会被释放：
        /// 只有一个异常时，在全部对象释放完毕后重新抛出该异常；有多个异常时抛出 <see cref="AggregateException"/>
        /// </summary>
        public void Dispose()
        {
            List<Exception>? exceptions = null;

            while (stack.Count > 0)
            {
                var node = stack.Pop();

                try
                {
                    if (node.Disposable != null)
                        node.Disposable.Dispose();
                    else
                        node.DisposeAction?.Invoke();
                }
                catch (Exception exception)
                {
                    (exceptions ??= new()).Add(exception);
                }
            }

            if (exceptions == null)
                return;

            if (exceptions.Count == 1)
                throw exceptions[0];

            throw new AggregateException(exceptions);
        }

        #region Details

        readonly Stack<Node> stack;

        private readonly struct Node
        {
            public readonly IDisposable? Disposable;
            public readonly Action? DisposeAction;

            public Node(IDisposable disposable)
            {
                Disposable = disposable;
                DisposeAction = null;
            }

            public Node(Action disposeAction)
            {
                DisposeAction = disposeAction;
                Disposable = null;
            }
        }

        #endregion
    }
}
