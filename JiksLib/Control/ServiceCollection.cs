using System;
using System.Collections.Generic;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 服务集合
    /// 以类型为键注册服务，支持沿 fallback 链向上查找。
    /// 与旧的静态 ServiceLocator 不同，服务集合是普通对象，依赖通过构造函数显式传入。
    /// 非线程安全：同一个集合及其 fallback 链只应在一个线程（通常是主线程）上使用。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 注册键是调用方显式指定的类型：<see cref="Register{T}(T, bool)"/> 以 <c>typeof(T)</c> 为键，
    /// 因此可以注册接口；<see cref="Register(object, bool)"/> 以运行时类型为键。
    /// 同一集合内重复注册同一键时，后注册的覆盖先注册的（以最后一次为准）；
    /// 旧实例若实现了 <see cref="IDisposable"/> 且要求自动释放，仍会在 <see cref="Dispose()"/> 时被释放。
    /// </para>
    /// <para>
    /// 查找只做精确类型匹配，不做“唯一可赋值类型”推断，避免多实现时的歧义；
    /// 需要按接口取用时，请在注册时使用 <see cref="Register{T}(T, bool)"/> 指定接口类型。
    /// </para>
    /// <para>
    /// <see cref="Register(object, bool)"/> / <see cref="Register{T}(T, bool)"/> 默认持有并释放
    /// 实现了 <see cref="IDisposable"/> 的服务；传 <c>autoDispose: false</c> 表示由调用方负责释放。
    /// <see cref="AttachDisposable"/> 与 <see cref="RunOnDispose"/> 用于追加额外的释放动作。
    /// fallback 集合始终不会被本集合释放。
    /// </para>
    /// </remarks>
    public abstract class ServiceCollection : IDisposable
    {
        /// <summary>
        /// 创建一个服务集合
        /// </summary>
        /// <param name="fallback">
        /// 查找失败时继续向上查找的集合；为 null 表示没有 fallback。
        /// fallback 的生命周期由调用方管理，本集合不会释放它。
        /// </param>
        protected ServiceCollection(ServiceCollection? fallback = null)
        {
            services = new();
            disposableStack = new();
            this.fallback = fallback;
        }

        /// <summary>
        /// 以运行时类型为键注册服务
        /// 同一键重复注册时，后注册的覆盖先注册的。
        /// </summary>
        /// <param name="service">要注册的服务</param>
        /// <param name="autoDispose">
        /// 为 true 时，服务若实现 <see cref="IDisposable"/>，会在本集合释放时被释放
        /// </param>
        /// <exception cref="ArgumentNullException">当 <paramref name="service"/> 为 null 时抛出</exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        protected void Register(object service, bool autoDispose = true)
        {
            RegisterCore(
                service.ThrowIfNull(nameof(service)).GetType(),
                service,
                autoDispose);
        }

        /// <summary>
        /// 以 <c>typeof(T)</c> 为键注册服务
        /// 需要按接口取用时使用这个重载；同一键重复注册时，后注册的覆盖先注册的。
        /// </summary>
        /// <typeparam name="T">注册键类型</typeparam>
        /// <param name="service">要注册的服务</param>
        /// <param name="autoDispose">
        /// 为 true 时，服务若实现 <see cref="IDisposable"/>，会在本集合释放时被释放
        /// </param>
        /// <exception cref="ArgumentNullException">当 <paramref name="service"/> 为 null 时抛出</exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        protected void Register<T>(T service, bool autoDispose = true)
        {
            var checkedService = service.ThrowIfNull(nameof(service));

            RegisterCore(
                typeof(T),
                checkedService!,
                autoDispose);
        }

        /// <summary>
        /// 追加一个在本集合释放时调用的对象
        /// 释放顺序为注册顺序的逆序。
        /// </summary>
        /// <param name="disposable">要释放的对象</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="disposable"/> 为 null 时抛出</exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        protected void AttachDisposable(IDisposable disposable)
        {
            disposable.ThrowIfNull(nameof(disposable));
            ThrowIfDisposed();
            disposableStack.Add(disposable);
        }

        /// <summary>
        /// 追加一个在本集合释放时执行的动作
        /// 执行顺序为添加顺序的逆序。
        /// </summary>
        /// <param name="action">要执行的动作</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 null 时抛出</exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        protected void RunOnDispose(Action action)
        {
            action.ThrowIfNull(nameof(action));
            ThrowIfDisposed();
            disposableStack.Add(action);
        }

        /// <summary>
        /// 尝试获取服务；沿 fallback 链查找
        /// </summary>
        /// <param name="service">找到的服务；未找到时为 default</param>
        /// <returns>是否找到服务</returns>
        /// <exception cref="InvalidOperationException">
        /// 注册的实例与请求类型不兼容时抛出
        /// </exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        public bool TryGet<T>(out T? service)
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

            service = default;
            return false;
        }

        /// <summary>
        /// 获取服务；找不到时抛出异常
        /// </summary>
        /// <exception cref="InvalidOperationException">服务未注册时抛出</exception>
        /// <exception cref="ObjectDisposedException">集合已经被释放时抛出</exception>
        public T Get<T>()
        {
            if (TryGet<T>(out var service))
                return service!;

            throw new InvalidOperationException(
                $"Service not registered: {typeof(T)}.");
        }

        /// <summary>
        /// 释放本集合持有的服务与追加的释放动作
        /// 按注册/添加顺序的逆序执行；fallback 不会被释放。
        /// 可重复调用，释放后不能再注册或查找。
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

            try
            {
                disposableStack.Dispose();
            }
            finally
            {
                services.Clear();
                ownedDisposables.Clear();
            }
        }

        void RegisterCore(Type serviceType, object service, bool autoDispose)
        {
            ThrowIfDisposed();

            if (!serviceType.IsInstanceOfType(service))
                throw new ArgumentException(
                    "Service must be assignable to the registered type.",
                    nameof(service));

            services[serviceType] = service;

            if (!autoDispose || !(service is IDisposable disposable))
                return;

            // 同一个实例可能以多个键注册；只释放一次。
            for (int i = 0; i < ownedDisposables.Count; i++)
            {
                if (ReferenceEquals(ownedDisposables[i], disposable))
                    return;
            }

            ownedDisposables.Add(disposable);
            disposableStack.Add(disposable);
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

        readonly Dictionary<Type, object> services;
        readonly DisposableStack disposableStack;
        readonly List<IDisposable> ownedDisposables = new();
        readonly ServiceCollection? fallback;
        bool disposed;
    }
}
