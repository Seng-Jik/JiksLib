using System;
using System.Collections.Generic;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 服务集合
    /// 以类型为键显式注册服务，支持沿 fallback 链向上查找；
    /// 与旧版静态 ServiceLocator 不同，服务集合是普通对象，依赖通过构造函数显式传入。
    /// 非线程安全：同一个集合及其 fallback 链只应在一个线程（通常是主线程）上使用。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 注册键是调用方显式指定的类型：<see cref="Add{T}(T)"/> 以 <c>typeof(T)</c> 为键，
    /// 因此可以注册接口；<see cref="Add(object)"/> 以运行时类型为键。
    /// 同一集合内重复注册同一键时，后注册的覆盖先注册的；旧实例若实现了
    /// <see cref="IDisposable"/>，仍会在 <see cref="Dispose()"/> 时被释放。
    /// </para>
    /// <para>
    /// 查找只做精确类型匹配，不做“唯一可赋值类型”推断，避免多实现时的歧义；
    /// 需要按接口取用时，请在注册时使用 <see cref="Add{T}(T)"/> 指定接口类型。
    /// </para>
    /// <para>
    /// 通过 <see cref="Add(object)"/> / <see cref="Add{T}(T)"/> 注册的服务由本集合持有，
    /// <see cref="Dispose()"/> 会按注册的逆序释放其中实现了 <see cref="IDisposable"/> 的实例；
    /// 使用 <see cref="AddExternal(object)"/> 注册的服务不会被释放。fallback 集合始终不会被释放。
    /// </para>
    /// </remarks>
    public class ServiceCollection : IDisposable
    {
        /// <summary>
        /// 创建一个服务集合
        /// </summary>
        /// <param name="fallback">
        /// 查找失败时继续向上查找的集合；为 null 表示没有 fallback。
        /// fallback 的生命周期由调用方管理，本集合不会释放它。
        /// </param>
        public ServiceCollection(ServiceCollection? fallback = null)
        {
            this.fallback = fallback;
        }

        /// <summary>
        /// 以运行时类型为键注册服务，并由本集合持有
        /// </summary>
        /// <param name="service">要注册的服务</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="service"/> 为 null 时抛出</exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        protected void Add(object service)
        {
            service.ThrowIfNull(nameof(service));
            Set(service.GetType(), service, owns: true);
        }

        /// <summary>
        /// 以 <c>typeof(T)</c> 为键注册服务，并由本集合持有
        /// 需要按接口取用时使用这个重载。
        /// </summary>
        /// <typeparam name="T">注册键类型</typeparam>
        /// <param name="service">要注册的服务，必须可赋值给 <typeparamref name="T"/></param>
        /// <exception cref="ArgumentNullException">当 <paramref name="service"/> 为 null 时抛出</exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        protected void Add<T>(T service)
            where T : class
        {
            service.ThrowIfNull(nameof(service));
            Set(typeof(T), service, owns: true);
        }

        /// <summary>
        /// 以指定类型为键注册服务，并由本集合持有
        /// </summary>
        /// <param name="serviceType">注册键类型</param>
        /// <param name="service">要注册的服务，必须可赋值给 <paramref name="serviceType"/></param>
        /// <exception cref="ArgumentNullException">参数为 null 时抛出</exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        protected void Add(Type serviceType, object service)
        {
            service.ThrowIfNull(nameof(service));
            serviceType.ThrowIfNull(nameof(serviceType));
            Set(serviceType, service, owns: true);
        }

        /// <summary>
        /// 以运行时类型为键注册服务，但不持有它
        /// 适合注册由外部生命周期管理的单例（例如场景对象）。
        /// </summary>
        /// <exception cref="ArgumentNullException">当 <paramref name="service"/> 为 null 时抛出</exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        protected void AddExternal(object service)
        {
            service.ThrowIfNull(nameof(service));
            Set(service.GetType(), service, owns: false);
        }

        /// <summary>
        /// 以 <c>typeof(T)</c> 为键注册服务，但不持有它
        /// </summary>
        /// <exception cref="ArgumentNullException">当 <paramref name="service"/> 为 null 时抛出</exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        protected void AddExternal<T>(T service)
            where T : class
        {
            service.ThrowIfNull(nameof(service));
            Set(typeof(T), service, owns: false);
        }

        /// <summary>
        /// 获取服务；找不到时抛出异常
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// 服务未注册，或注册的实例与请求类型不兼容时抛出
        /// </exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        public T Get<T>()
            where T : class
        {
            if (TryGet<T>(out var service))
                return service!;

            throw new InvalidOperationException(
                $"Service not registered: {typeof(T)}.");
        }

        /// <summary>
        /// 尝试获取服务；沿 fallback 链查找
        /// </summary>
        /// <param name="service">找到的服务；未找到时为 null</param>
        /// <returns>是否找到服务</returns>
        /// <exception cref="InvalidOperationException">
        /// 注册的实例与请求类型不兼容时抛出
        /// </exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        public bool TryGet<T>(out T? service)
            where T : class
        {
            ThrowIfDisposed();

            for (ServiceCollection? current = this;
                 current != null;
                 current = current.fallback)
            {
                if (!current.services.TryGetValue(typeof(T), out var value))
                    continue;

                if (value is T typed)
                {
                    service = typed;
                    return true;
                }

                throw new InvalidOperationException(
                    $"Service registered for {typeof(T)} is not assignable to it.");
            }

            service = null;
            return false;
        }

        /// <summary>
        /// 释放本集合持有的服务（按注册的逆序），并清空注册表。
        /// 可重复调用；fallback 与 <see cref="AddExternal(object)"/> 注册的服务不会被释放。
        /// 单个服务释放抛出的异常会被聚合，全部释放完后再抛出。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放本集合持有的资源
        /// </summary>
        /// <param name="disposing">为 true 时释放托管资源</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            disposed = true;

            if (!disposing)
                return;

            List<Exception>? exceptions = null;

            for (int i = ownedDisposables.Count - 1; i >= 0; i--)
            {
                try
                {
                    ownedDisposables[i].Dispose();
                }
                catch (Exception exception)
                {
                    (exceptions ??= new()).Add(exception);
                }
            }

            ownedDisposables.Clear();
            services.Clear();

            if (exceptions != null)
                throw new AggregateException(exceptions);
        }

        void Set(Type serviceType, object service, bool owns)
        {
            ThrowIfDisposed();

            serviceType.ThrowIfNull(nameof(serviceType));

            if (!serviceType.IsInstanceOfType(service))
                throw new ArgumentException(
                    "Service must be assignable to the registered type.",
                    nameof(service));

            services[serviceType] = service;

            if (owns && service is IDisposable disposable)
            {
                for (int i = 0; i < ownedDisposables.Count; i++)
                {
                    if (ReferenceEquals(ownedDisposables[i], disposable))
                        return;
                }

                ownedDisposables.Add(disposable);
            }
        }

        void ThrowIfDisposed()
        {
#if NET7_0_OR_GREATER
            ObjectDisposedException.ThrowIf(disposed, this);
#else
            if (disposed)
                throw new ObjectDisposedException(nameof(ServiceCollection));
#endif
        }

        readonly ServiceCollection? fallback;
        readonly Dictionary<Type, object> services = new();
        readonly List<IDisposable> ownedDisposables = new();
        bool disposed;
    }
}
