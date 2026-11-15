using System;
using System.Collections.Generic;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 服务定位器
    /// 注意：如果你使用基于现代.NET版本的Unity，则应该在退出播放模式时Dispose所有的Scope
    /// </summary>
    public static class ServiceLocator
    {
        /// <summary>
        /// 服务作用域
        /// 包含一组服务，可在同时注册或取消注册
        /// </summary>
        public abstract class Scope
        {
            /// <summary>
            /// 注册服务，仅可在 OnRegisterServices 中调用
            /// </summary>
            protected void Register<T>(T service)
                where T : class
            {
                var unregisterStack = UnregisterStack
                    ?? throw new InvalidOperationException(
                        "Only allow call Register<T> in OnRegisterServices.");

                var old = ServiceHolder<T>.Service;
                unregisterStack.Add(() => ServiceHolder<T>.Service = old);
                ServiceHolder<T>.Service = service;
            }

            /// <summary>
            /// 需要注册服务时，Service Locator 会调用此方法
            /// 此方法需要将要注册的服务通过 <c>Register&lt;T&gt;</c> 方法注册
            /// </summary>
            protected internal abstract void OnRegisterServices();

            /// <summary>
            /// 本次注册产生的注销动作栈；仅在 OnRegisterServices 执行期间非 null
            /// </summary>
            internal DisposableStack? UnregisterStack;
        }

        /// <summary>
        /// 进入作用域
        /// 传入作用域对象，并返回一个 IDisposable 对象，用于退出作用域
        /// 退出作用域的顺序必须和进入作用域一致
        /// 只允许在主线程上调用此函数及返回的 IDisposable，在其他线程上调用会产生未定义行为
        /// 返回的 IDisposable 对象必须按照 EnterScope 的调用顺序的逆序调用
        /// </summary>
        /// <exception cref="ArgumentNullException">当 <paramref name="scope"/> 为 null 时抛出</exception>
        public static IDisposable EnterScope(Scope scope)
        {
            scope.ThrowIfNull(nameof(scope));

            DisposableStack unregisterStack;

            try
            {
                unregisterStack = new DisposableStack();
                scope.UnregisterStack = unregisterStack;
                scope.OnRegisterServices();
                scopes.Add(new ScopeEntry(scope, unregisterStack));
            }
            catch (Exception exception)
            {
                // 注册失败：按注册的逆序回滚已注册的服务，避免半注册状态泄漏。
                // 回滚本身再抛异常时保留原始异常（聚合抛出），不让回滚覆盖它。
                try
                {
                    scope.UnregisterStack?.Dispose();
                }
                catch (Exception rollbackException)
                {
                    throw new AggregateException(exception, rollbackException);
                }

                throw;
            }
            finally
            {
                scope.UnregisterStack = null;
            }

            return Disposable.FromAction(() =>
            {
                if (scopes.Count == 0 || scopes[scopes.Count - 1].Scope != scope)
                    throw new InvalidOperationException(
                        "Dispose service locator scope must in order.");

                try
                {
                    // DisposableStack 会按注册的逆序执行注销，并保证单个动作失败不影响其余动作
                    unregisterStack.Dispose();
                }
                finally
                {
                    // 即使注销过程抛出异常，也要把作用域出栈，避免后续退出被卡住
                    scopes.RemoveAt(scopes.Count - 1);
                }
            });
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        public static T Get<T>() where T : class =>
            ServiceHolder<T>.Service
                ?? throw new InvalidOperationException(
                        "Service not registered.");

        /// <summary>
        /// 当前仍处于活动状态（已 EnterScope 且尚未 Dispose）的作用域数量
        /// </summary>
        public static int ActiveScopeCount => scopes.Count;

        /// <summary>
        /// 释放所有仍处于活动状态的作用域，按进入顺序的逆序逐个注销。
        /// 单个作用域的注销异常会被收集，全部处理完后以 <see cref="AggregateException"/> 抛出。
        /// </summary>
        public static void Reset()
        {
            List<Exception>? exceptions = null;

            for (int i = scopes.Count - 1; i >= 0; --i)
            {
                try
                {
                    scopes[i].UnregisterStack.Dispose();
                }
                catch (Exception exception)
                {
                    (exceptions ??= new()).Add(exception);
                }
            }

            scopes.Clear();

            if (exceptions != null)
                throw new AggregateException(exceptions);
        }

        readonly struct ScopeEntry
        {
            public readonly Scope Scope;
            public readonly DisposableStack UnregisterStack;

            public ScopeEntry(Scope scope, DisposableStack unregisterStack)
            {
                Scope = scope;
                UnregisterStack = unregisterStack;
            }
        }

        static readonly List<ScopeEntry> scopes = new();

        private static class ServiceHolder<T>
            where T : class
        {
            public static volatile T? Service;
        }
    }
}
