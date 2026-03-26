using System;
using System.Collections.Generic;

namespace JiksLib.Control
{
    /// <summary>
    /// 服务定位器
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
                UnregisterActions += () => ServiceHolder<T>.Service = old;
                ServiceHolder<T>.Service = service;
            }

            /// <summary>
            /// 需要注册服务时，Service Locator 会调用此方法
            /// 此方法需要将要注册的服务通过 Register<T> 方法注册
            /// </summary>
            protected internal abstract void OnRegisterServices();

            internal Action? UnregisterActions;
        }

        /// <summary>
        /// 进入作用域
        /// 传入作用域对象，并返回一个 IDisposable 对象，用于退出作用域
        /// 退出作用域的顺序必须和进入作用域一致
        /// 只允许在主线程上调用此函数及返回的 IDisposable，在其他线程上调用会产生未定义行为
        /// 返回的 IDisposable 对象必须按照 EnterScope 的调用顺序的逆序调用
        /// </summary>
        public static IDisposable EnterScope(Scope scope)
        {
            Action unregisterActions;

            try
            {
                scope.UnregisterActions = static () => { };
                scope.OnRegisterServices();
                unregisterActions = scope.UnregisterActions;
                scopes.Push(scope);
            }
            catch
            {
                scope.UnregisterActions!();
                throw;
            }
            finally
            {
                scope.UnregisterActions = null;
            }

            return Disposable.FromAction(() =>
            {
                if (scopes.Count == 0 || scopes.Peek() != scope)
                    throw new InvalidOperationException(
                        "Dispose service locator scope must in order.");

                unregisterActions();
                scopes.Pop();
            });
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        public static T Get<T>() where T : class =>
            ServiceHolder<T>.Service
                ?? throw new InvalidOperationException(
                        "Service not registered.");

        static readonly Stack<Scope> scopes = new();

        private static class ServiceHolder<T>
            where T : class
        {
            public static volatile T? Service;
        }
    }
}
