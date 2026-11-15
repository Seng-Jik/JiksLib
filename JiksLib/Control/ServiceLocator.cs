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
                if (UnregisterActions == null)
                    throw new InvalidOperationException(
                        "Only allow call Register<T> in OnRegisterServices.");

                var old = ServiceHolder<T>.Service;
                UnregisterActions.Add(() => ServiceHolder<T>.Service = old);
                ServiceHolder<T>.Service = service;
            }

            /// <summary>
            /// 需要注册服务时，Service Locator 会调用此方法
            /// 此方法需要将要注册的服务通过 <c>Register&lt;T&gt;</c> 方法注册
            /// </summary>
            protected internal abstract void OnRegisterServices();

            internal List<Action>? UnregisterActions;
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

            List<Action> unregisterActions;

            try
            {
                scope.UnregisterActions = new();
                scope.OnRegisterServices();
                unregisterActions = scope.UnregisterActions;
                scopes.Add(new ScopeEntry(scope, unregisterActions));
            }
            catch
            {
                // 注册失败：按注册的逆序回滚已注册的服务，避免半注册状态泄漏
                RunUnregisterActions(scope.UnregisterActions);
                throw;
            }
            finally
            {
                scope.UnregisterActions = null;
            }

            return Disposable.FromAction(() =>
            {
                if (scopes.Count == 0 || scopes[scopes.Count - 1].Scope != scope)
                    throw new InvalidOperationException(
                        "Dispose service locator scope must in order.");

                RunUnregisterActions(unregisterActions);
                scopes.RemoveAt(scopes.Count - 1);
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
        /// 释放所有仍处于活动状态的作用域，按进入顺序的逆序执行各自的注销委托。
        /// 主要用于测试隔离与退出流程；单个注销委托抛出的异常会被收集，全部执行完后
        /// 以 <see cref="AggregateException"/> 抛出。
        /// </summary>
        public static void Reset()
        {
            List<Exception>? exceptions = null;

            for (int i = scopes.Count - 1; i >= 0; --i)
            {
                try
                {
                    RunUnregisterActions(scopes[i].UnregisterActions);
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

        /// <summary>
        /// 逆序执行注销委托：后注册的服务先恢复，保证同一类型覆盖注册后能回到进入作用域前的值。
        /// </summary>
        static void RunUnregisterActions(List<Action>? actions)
        {
            if (actions == null)
                return;

            for (int i = actions.Count - 1; i >= 0; --i)
                actions[i]();
        }

        readonly struct ScopeEntry
        {
            public readonly Scope Scope;
            public readonly List<Action> UnregisterActions;

            public ScopeEntry(Scope scope, List<Action> unregisterActions)
            {
                Scope = scope;
                UnregisterActions = unregisterActions;
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
