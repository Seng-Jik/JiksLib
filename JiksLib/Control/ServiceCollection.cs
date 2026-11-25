using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    /// 同一实例以多个键注册时会按注册次数各记一次、被释放多次（刻意不做去重），
    /// 需要只释放一次时请改用 <c>autoDispose: false</c> 自行管理。
    /// <see cref="AttachDisposable"/> 与 <see cref="RunOnDispose"/> 用于追加额外的释放动作。
    /// fallback 集合始终不会被本集合释放。
    /// </para>
    /// </remarks>
    [SuppressMessage(
        "Design",
        "CA1063",
        Justification = "本类不持有非托管资源，Dispose 幂等且刻意设计为释放后可继续当空集合复用，" +
                        "因此不提供 Dispose(bool) 模板；同时它是 abstract 基类，无法按规则建议标记为 sealed。")]
    [SuppressMessage(
        "Design",
        "CA1816",
        Justification = "本类自身不引入终结器也不持有非托管资源，刻意不调用 GC.SuppressFinalize；" +
                        "需要终结器的派生类型应自行在 Dispose 中抑制。")]
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
        protected void Register(object service, bool autoDispose = true) =>
            RegisterCore(
                service.ThrowIfNull(nameof(service)).GetType(),
                service,
                autoDispose);

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
        protected void Register<T>(T service, bool autoDispose = true) =>
            RegisterCore(
                typeof(T),
                service.ThrowIfNull(nameof(service))!,
                autoDispose);

        /// <summary>
        /// 追加一个在本集合释放时调用的对象
        /// 释放顺序为注册顺序的逆序。
        /// </summary>
        /// <param name="disposable">要释放的对象</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="disposable"/> 为 null 时抛出</exception>
        protected void AttachDisposable(IDisposable disposable) =>
            disposableStack.Add(disposable.ThrowIfNull(nameof(disposable)));

        /// <summary>
        /// 追加一个在本集合释放时执行的动作
        /// 执行顺序为添加顺序的逆序。
        /// </summary>
        /// <param name="action">要执行的动作</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="action"/> 为 null 时抛出</exception>
        protected void RunOnDispose(Action action) =>
            disposableStack.Add(action.ThrowIfNull(nameof(action)));

        /// <summary>
        /// 尝试获取服务；沿 fallback 链查找
        /// </summary>
        /// <param name="service">找到的服务；未找到时为 default</param>
        /// <returns>是否找到服务</returns>
        /// <exception cref="InvalidOperationException">
        /// 注册的实例与请求类型不兼容时抛出
        /// </exception>
        public bool TryGet<T>(out T? service)
        {
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
        public T Get<T>()
        {
            if (TryGet<T>(out var service))
                return service!;

            throw new InvalidOperationException(
                $"Service not registered: {typeof(T)}.");
        }

        /// <summary>
        /// 释放本集合持有的服务与追加的释放动作
        /// 按注册/添加顺序的逆序执行；fallback 不会被释放；可重复调用。
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Dispose 不会让集合失效，释放后仍可作为空的 ServiceCollection 继续使用</b>：
        /// 集合回到"空但可用"的状态——查找按未注册处理（<see cref="TryGet{T}"/> 返回 false，
        /// <see cref="Get{T}"/> 抛 <see cref="InvalidOperationException"/>），
        /// 仍可继续注册服务、<c>AttachDisposable</c> 与 <c>RunOnDispose</c>，
        /// 这些新增项会在下一次 <see cref="Dispose()"/> 时照常释放。
        /// 因此本类不抛 <see cref="ObjectDisposedException"/>，调用方不必为"已释放"单独维护状态判断。
        /// </para>
        /// <para>
        /// 唯一例外是释放期间的重入：某个服务在自己的 <see cref="IDisposable.Dispose"/> 里回写本集合，
        /// 会因 <see cref="DisposableStack"/> 禁止释放期间添加而失败。
        /// </para>
        /// </remarks>
        public void Dispose()
        {
            try
            {
                disposableStack.Dispose();
            }
            finally
            {
                services.Clear();
            }
        }

        void RegisterCore(Type serviceType, object service, bool autoDispose)
        {
            services[serviceType] = service;

            if (autoDispose && service is IDisposable disposable)
                disposableStack.Add(disposable);
        }

        readonly Dictionary<Type, object> services;
        readonly DisposableStack disposableStack;
        readonly ServiceCollection? fallback;
    }
}
