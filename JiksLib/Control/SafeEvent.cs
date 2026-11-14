using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 安全事件处理器委托
    /// 所谓“安全”，是指事件处理器在执行过程中遇到异常不会中断事件的发布过程，异常会被收集到异常输出列表中
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    /// <param name="event">事件对象</param>
    public delegate void EventHandler<TEvent>(TEvent @event);

    /// <summary>
    /// 安全事件发布器
    /// </summary>
    public interface ISafeEventPublisher<in TEvent>
    {
        /// <summary>
        /// 安全地发布事件
        /// </summary>
        /// <param name="event">事件对象</param>
        /// <param name="exceptionsOutput">输出的事件处理过程中发生的异常，若为null则忽略异常</param>
        void Publish(
            TEvent @event,
            ICollection<Exception>? exceptionsOutput);
    }

    /// <summary>
    /// 安全事件包装器
    /// 提供可配置的重入策略与异常隔离：
    /// 默认禁止在发布期间增删监听器、禁止在发布期间再次发布；
    /// 开启后，监听器增删会延迟到整个最外层发布结束后生效，
    /// 重复发布则进入 FIFO 队列，由最外层发布在当前监听器集合执行完后依次派发。
    /// 仅支持在主线程上使用，不支持多线程并发访问。
    /// </summary>
    /// <typeparam name="TEvent">事件类型</typeparam>
    public sealed class SafeEvent<TEvent>
    {
        /// <summary>
        /// 构造安全事件包装器
        /// </summary>
        /// <param name="publisher">输出的事件发布器</param>
        /// <param name="allowAddRemoveListenerDuringInvoke">
        /// 是否允许在发布期间添加或移除监听器。
        /// 为 false（默认）时，发布期间调用 <see cref="AddListener"/> 或
        /// <see cref="RemoveListener"/> 会抛出 <see cref="InvalidOperationException"/>；
        /// 为 true 时，这些操作会延迟到整个最外层发布（包括递归队列排空）结束后生效。
        /// </param>
        /// <param name="allowRecursiveInvoke">
        /// 是否允许在发布期间再次发布事件。
        /// 为 false（默认）时，嵌套发布会抛出 <see cref="InvalidOperationException"/>；
        /// 为 true 时，嵌套发布不会递归调用，而是把事件加入 FIFO 队列，
        /// 由最外层发布在当前监听器集合执行完后依次派发。
        /// 启用后调用方必须保证监听器最终停止再次发布：本类型不限制队列长度与派发次数，
        /// 线性自递归可能导致发布永不返回，分支递归可能使队列持续增长并耗尽内存。
        /// </param>
        /// <param name="capacity">监听器存储的预分配容量，必须为非负数。</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 当<paramref name="capacity"/>为负数时抛出。
        /// </exception>
        /// <remarks>
        /// 递归发布使用队列展平，不会因嵌套发布增长调用栈，
        /// 因此同一个 <see cref="SafeEvent{TEvent}"/> 实例的深递归发布不会抛出
        /// <see cref="StackOverflowException"/>；该保证不适用于不同实例之间的嵌套发布。
        /// </remarks>
        public SafeEvent(
            out Publisher publisher,
            bool allowAddRemoveListenerDuringInvoke = false,
            bool allowRecursiveInvoke = false,
            int capacity = 0)
        {
            if (capacity > 0)
            {
                listeners = new(capacity);
            }
            else if (capacity == 0)
            {
                listeners = new();
            }
            else
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Capacity cannot be negative.");
            }

            publisher = new(this);

            if (allowAddRemoveListenerDuringInvoke)
                listenersDelayedOperation = new();

            if (allowRecursiveInvoke)
                recursiveInvokeQueue = new();
        }

        /// <summary>
        /// 添加事件监听器。
        /// 发布期间是否允许调用由构造函数参数 <c>allowAddRemoveListenerDuringInvoke</c> 决定：
        /// 允许时，本次添加会延迟到整个最外层发布（包括递归队列排空）结束后生效，
        /// 因此当前发布不会调用该监听器，<see cref="ListenerCount"/> 也在那之后才增加；
        /// 不允许时，发布期间调用会抛出 <see cref="InvalidOperationException"/>。
        /// 同一监听器可以重复添加，每次添加都会独立计数并在每次发布时各被调用一次。
        /// </summary>
        /// <param name="listener">事件监听器</param>
        /// <exception cref="ArgumentNullException">
        /// 当<paramref name="listener"/>为null时抛出
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// 发布进行中且构造时未允许增删监听器时抛出。
        /// 若该调用发生在监听器回调内且未自行捕获，异常会由正在执行的 Publish 收集到
        /// 其 <c>exceptionsOutput</c>；对于排队的嵌套事件，则写入发起该次嵌套 Publish
        /// 时传入的集合。异常不会从 Publish 抛出。
        /// </exception>
        public void AddListener(EventHandler<TEvent> listener)
        {
            listener.ThrowIfNull(nameof(listener));

            if (invoking)
            {
                if (listenersDelayedOperation == null)
                    throw new InvalidOperationException(
                        "Adding or removing listeners during event invocation is not allowed.");

                listenersDelayedOperation.Enqueue((true, listener));
            }
            else
            {
                listeners.Add(listener);
                removeIndex = listeners.Count - 1;
                ++ListenerCount;
            }
        }

        /// <summary>
        /// 移除事件监听器。
        /// 发布期间是否允许调用由构造函数参数 <c>allowAddRemoveListenerDuringInvoke</c> 决定：
        /// 允许时，本次移除会延迟到整个最外层发布（包括递归队列排空）结束后生效，
        /// 因此该监听器仍会参与当前发布及排队的嵌套事件，
        /// <see cref="ListenerCount"/> 也在那之后才减少；
        /// 不允许时，发布期间调用会抛出 <see cref="InvalidOperationException"/>。
        /// 同一监听器被重复添加时，一次调用只移除其中一个匹配项；未注册的监听器不会产生任何效果。
        /// </summary>
        /// <param name="listener">要移除的事件监听器</param>
        /// <exception cref="ArgumentNullException">
        /// 当<paramref name="listener"/>为null时抛出
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// 发布进行中且构造时未允许增删监听器时抛出。
        /// 若该调用发生在监听器回调内且未自行捕获，异常会由外层发布收集到
        /// <c>exceptionsOutput</c>，不会从外层 <c>Publish</c> 抛出。
        /// </exception>
        public void RemoveListener(EventHandler<TEvent> listener)
        {
            listener.ThrowIfNull(nameof(listener));

            if (invoking)
            {
                if (listenersDelayedOperation == null)
                    throw new InvalidOperationException(
                        "Adding or removing listeners during event invocation is not allowed.");

                listenersDelayedOperation.Enqueue((false, listener));
            }
            else
            {
                var listenerCount = listeners.Count;

                if (listenerCount == 0) return;

                if (removeIndex >= listenerCount || removeIndex < 0)
                    removeIndex = listenerCount - 1;

                if (listeners[removeIndex] == listener)
                {
                    listeners[removeIndex] = null;
                    --ListenerCount;
                    return;
                }

                int offset = 1;
                for (; ; ++offset)
                {
                    int index1 = removeIndex + offset;
                    int index2 = removeIndex - offset;

                    if (index1 < listenerCount)
                    {
                        if (listeners[index1] == listener)
                        {
                            listeners[index1] = null;
                            --ListenerCount;
                            removeIndex = index1 + 1;
                            return;
                        }
                    }
                    else
                    {
                        goto BACK_ONLY;
                    }

                    if (index2 >= 0)
                    {
                        if (listeners[index2] == listener)
                        {
                            listeners[index2] = null;
                            --ListenerCount;
                            removeIndex = index2 - 1;
                            return;
                        }
                    }
                    else
                    {
                        goto FRONT_ONLY;
                    }
                }

                BACK_ONLY:

                for (int i = removeIndex - offset; i >= 0; --i)
                {
                    if (listeners[i] != listener) continue;
                    listeners[i] = null;
                    --ListenerCount;
                    removeIndex = i - 1;
                    return;
                }

                FRONT_ONLY:

                for (int i = offset + removeIndex; i < listenerCount; ++i)
                {
                    if (listeners[i] != listener) continue;
                    listeners[i] = null;
                    --ListenerCount;
                    removeIndex = i + 1;
                    return;
                }
            }
        }

        /// <summary>
        /// 获取当前已生效的监听器数量。
        /// 重复添加的同一监听器按注册次数重复计数。
        /// 在发布期间：若允许增删，待添加的监听器不计入、待移除的监听器仍计入，
        /// 直到整个最外层发布（包括递归队列排空）结束后才更新；
        /// 若不允许增删，发布期间的添加/移除会抛出异常，计数保持不变。
        /// </summary>
        public int ListenerCount { get; private set; }

        /// <summary>
        /// 事件发布器
        /// 用于安全地触发事件
        /// </summary>
        public readonly struct Publisher : ISafeEventPublisher<TEvent>
        {
            /// <summary>
            /// 安全触发事件。
            /// 依次调用当前监听器，监听器同步抛出的异常会被收集到
            /// <paramref name="exceptionsOutput"/>，不会中断本次发布。
            /// </summary>
            /// <param name="event">事件对象</param>
            /// <param name="exceptionsOutput">
            /// 异常输出集合；监听器同步抛出的异常会写入此集合，为 null 则忽略异常。
            /// 排队派发的嵌套事件写入发起该次嵌套发布时传入的集合；
            /// 嵌套发布已返回，因此该集合要等最外层发布排空队列后才填充。
            /// </param>
            /// <remarks>
            /// <para>
            /// 监听器回调中同步抛出的任何异常都会进入 <paramref name="exceptionsOutput"/>，
            /// 包括监听器内调用 <c>AddListener</c> / <c>RemoveListener</c> 抛出的异常，
            /// 以及不允许递归发布时嵌套 <c>Publish</c> 抛出的异常。
            /// 异常隔离只覆盖同步调用；<c>async void</c> 监听器在 await 之后的异常不在此范围。
            /// </para>
            /// <para>
            /// 默认不允许递归发布，嵌套 <c>Publish</c> 会抛出
            /// <see cref="InvalidOperationException"/>。启用 <c>allowRecursiveInvoke</c> 后，
            /// 嵌套发布只把事件加入 FIFO 队列并立即返回，由最外层发布在当前监听器集合
            /// 执行完后依次派发；排队事件的监听器再次发布时继续入队。
            /// 队列展平不会增长调用栈，因此同一个实例的深递归发布不会产生
            /// <see cref="StackOverflowException"/>。
            /// </para>
            /// <para>
            /// 队列长度和派发次数没有上限，调用方必须保证监听器最终停止再次发布：
            /// 线性自递归可能队列很小但发布永不返回，分支递归可能使队列持续增长并耗尽内存。
            /// 该保证仅针对同一个 SafeEvent 实例；不同实例之间的嵌套发布仍是同步调用。
            /// class 事件在队列中保存的是引用，排空前修改事件对象会被后续监听器观察到。
            /// </para>
            /// <para>
            /// 发布期间增删监听器的行为由 <c>allowAddRemoveListenerDuringInvoke</c> 决定：
            /// 允许时延迟到整个最外层发布结束后生效；不允许时抛出
            /// <see cref="InvalidOperationException"/>，并被收集到
            /// <paramref name="exceptionsOutput"/>。
            /// </para>
            /// </remarks>
            public readonly void Publish(
                TEvent @event,
                ICollection<Exception>? exceptionsOutput)
            {
                if (this.@event.invoking)
                {
                    if (this.@event.recursiveInvokeQueue == null)
                        throw new InvalidOperationException(
                            "Recursive invocation of SafeEvent is not allowed.");

                    this.@event.recursiveInvokeQueue.Enqueue(
                        (@event, exceptionsOutput));

                    return;
                }

                try
                {
                    this.@event.invoking = true;
                    var listeners = this.@event.listeners;
                    int rewriteIndex = 0;
                    int listenersCount = listeners.Count;

                    for (int i = 0; i < listenersCount; ++i)
                    {
                        var listener = listeners[i];

                        if (listener == null)
                            continue;

                        listeners[i] = null;
                        listeners[rewriteIndex++] = listener;

                        try
                        {
                            listener(@event);
                        }
                        catch (Exception ex)
                        {
                            exceptionsOutput?.Add(ex);
                        }
                    }

                    if (rewriteIndex < listeners.Count)
                    {
                        listeners.RemoveRange(
                            rewriteIndex,
                            listeners.Count - rewriteIndex);
                    }

                    if (this.@event.recursiveInvokeQueue != null)
                    {
                        while (this.@event.recursiveInvokeQueue.Count > 0)
                        {
                            (var recEvent, var exnOut) =
                                this.@event.recursiveInvokeQueue.Dequeue();

                            foreach (var listener in listeners)
                            {
                                try
                                {
                                    listener!(recEvent);
                                }
                                catch (Exception ex)
                                {
                                    exnOut?.Add(ex);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    this.@event.invoking = false;
                    this.@event.ApplyDelayedListenerAddOrRemove();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Publisher(SafeEvent<TEvent> @event)
            {
                this.@event = @event;
            }

            readonly SafeEvent<TEvent> @event;
        }

        /// <summary>监听器列表；删除只置 null，下一次发布时压缩。</summary>
        readonly List<EventHandler<TEvent>?> listeners;

        /// <summary>是否处于发布会话中；会话会覆盖递归队列的整个排空过程。</summary>
        bool invoking = false;

        /// <summary>移除监听器时的反向扫描起点提示。</summary>
        int removeIndex = 0;

        /// <summary>
        /// 递归发布排队的事件及其异常输出；未启用递归发布时为 null。
        /// </summary>
        readonly Queue<(TEvent Event, ICollection<Exception>? ExceptionsOutput)>?
            recursiveInvokeQueue;

        /// <summary>
        /// 发布期间产生的监听器增删操作；未启用发布期间增删时为 null。
        /// </summary>
        readonly Queue<(bool isAddOrRemove, EventHandler<TEvent>)>?
            listenersDelayedOperation;

        /// <summary>
        /// 应用发布期间排队的监听器增删操作。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void ApplyDelayedListenerAddOrRemove()
        {
            if (listenersDelayedOperation == null) return;

            while (listenersDelayedOperation!.Count > 0)
            {
                var op = listenersDelayedOperation.Dequeue();

                if (op.isAddOrRemove) AddListener(op.Item2);
                else RemoveListener(op.Item2);
            }
        }
    }
}
