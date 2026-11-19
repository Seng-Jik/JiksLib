using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 有限状态机
    /// </summary>
    /// <remarks>
    /// <para>
    /// 生命周期回调（<c>OnBuild</c>、<c>OnExit</c>、<c>OnStateSwitch</c>、<c>OnEnter</c>、<c>OnReset</c>）
    /// 与 <see cref="Switch(TTransition)"/> / <see cref="Switch(TState)"/> / <see cref="Reset"/> 共用一把
    /// "可切换"闸门：构建（含初始 OnEnter）、状态切换、重置期间调用 Switch 或 Reset 会抛出
    /// <see cref="InvalidOperationException"/>，避免回调内重入导致 OnEnter / OnExit 配对错乱。
    /// </para>
    /// <para>
    /// 回调内抛出的异常会正常向外传播；状态切换本身仍会完成（新状态的 OnEnter 在 finally 中执行）。
    /// </para>
    /// </remarks>
    public sealed class FSM<TState, TStateArg, TTransition>
        where TState : class, FSM<TState, TStateArg, TTransition>.IState
        where TTransition : struct
    {
        /// <summary>
        /// 构造器
        /// 只能构建一次状态机；构建后构造器进入只读状态，再次调用 <see cref="Build"/>
        /// 或任何 Add* 方法都会抛 <see cref="InvalidOperationException"/>。
        /// 需要重复构建或复用配置时，用复制构造函数复制一个副本。
        /// </summary>
        public sealed class Builder
        {
            /// <summary>
            /// 创建状态机构造器
            /// </summary>
            /// <param name="startState">初始状态</param>
            /// <param name="defaultState">当找不到Transition时跳转到此状态</param>
            /// <exception cref="ArgumentNullException">当 <paramref name="startState"/> 为 null 时抛出</exception>
            public Builder(TState startState, TState? defaultState = null)
            {
                this.startState = startState.ThrowIfNull(nameof(startState));
                AddState(this.startState);

                this.defaultState = defaultState;

                // startState 与 defaultState 相同是合法配置，这时只注册一次，
                // 否则 Dictionary.Add 会因重复键抛异常。
                if (defaultState != null && !ReferenceEquals(defaultState, this.startState))
                    AddState(defaultState);
            }

            /// <summary>
            /// 复制一个构造器
            /// 副本拥有独立的状态表与转移表，可以继续修改并独立构建；
            /// 无论源构造器是否已经构建过，副本都处于"未构建"状态。
            /// </summary>
            /// <param name="other">要复制的构造器</param>
            /// <exception cref="ArgumentNullException">当 <paramref name="other"/> 为 null 时抛出</exception>
            public Builder(Builder other)
            {
                other.ThrowIfNull(nameof(other));

                startState = other.startState;
                defaultState = other.defaultState;
                states = CopyStates(other.states);
                anyTimeTransitions = other.anyTimeTransitions == null
                    ? null
                    : new Dictionary<TTransition, TState>(other.anyTimeTransitions);
            }

            /// <summary>
            /// 添加状态
            /// 重复添加同一状态是幂等的
            /// </summary>
            /// <param name="state">状态对象</param>
            /// <exception cref="InvalidOperationException">构造器已经构建过时抛出</exception>
            public void AddState(TState state)
            {
                ThrowIfBuilt();

                state.ThrowIfNull(nameof(state));

                if (!states.ContainsKey(state))
                    states.Add(state, null);
            }

            /// <summary>
            /// 添加状态转移
            /// 如果同时使用 AddAnyTimeTransition 定义了任意状态下的转移
            /// 则优先使用 AddTransition 定义的转移
            /// </summary>
            /// <param name="prevState">来源状态，必须先调用 AddState</param>
            /// <param name="transition">状态转移</param>
            /// <param name="nextState">目标状态，必须先调用 AddState</param>
            /// <exception cref="InvalidOperationException">构造器已经构建过时抛出</exception>
            public void AddTransition(
                TState prevState,
                TTransition transition,
                TState nextState)
            {
                ThrowIfBuilt();

                if (!states.ContainsKey(nextState))
                    throw new ArgumentException(
                        "State not found, call AddState first.", nameof(nextState));

                if (!states.ContainsKey(prevState))
                    throw new ArgumentException
                        ("State not found, call AddState first.", nameof(prevState));

                (states[prevState] ??= new()).Add(transition, nextState);
            }

            /// <summary>
            /// 添加任意状态下的状态转移
            /// 如果同时使用 AddTransition 定义了特定状态下的转移
            /// 则优先使用 AddTransition 定义的转移
            /// </summary>
            /// <param name="transition">状态转移</param>
            /// <param name="nextState">目标状态，必须先调用 AddState</param>
            /// <exception cref="InvalidOperationException">构造器已经构建过时抛出</exception>
            public void AddAnyTimeTransition(
                TTransition transition,
                TState nextState)
            {
                ThrowIfBuilt();

                if (!states.ContainsKey(nextState))
                    throw new ArgumentException(
                        "State not found, call AddState first.", nameof(nextState));

                (anyTimeTransitions ??= new()).Add(transition, nextState);
            }

            /// <summary>
            /// 构建有限状态机
            /// 该操作会把状态表与转移表的所有权转移给状态机，不再复制；
            /// 构建成功后构造器进入只读状态，再次调用 Build 或任何 Add* 方法都会抛
            /// <see cref="InvalidOperationException"/>。需要再次构建时，
            /// 先用复制构造函数复制一个 Builder。
            /// </summary>
            /// <exception cref="InvalidOperationException">当前构造器已经构建过时抛出</exception>
            public FSM<TState, TStateArg, TTransition> Build(TStateArg arg)
            {
                ThrowIfBuilt();

                // 直接把表交给状态机（所有权转移）。因为之后构造器会被冻结，
                // 不存在"构建后继续修改影响已构建状态机"的问题。
                var fsm = new FSM<TState, TStateArg, TTransition>(
                    states,
                    anyTimeTransitions,
                    defaultState,
                    startState,
                    arg);

                built = true;
                return fsm;
            }

            /// <summary>
            /// 构建后禁止继续修改或再次构建
            /// </summary>
            void ThrowIfBuilt()
            {
                if (built)
                    throw new InvalidOperationException(
                        "Builder 已经构建过状态机，不能再次修改或构建；" +
                        "如需重新构建，请使用复制构造函数创建一个新的 Builder。");
            }

            /// <summary>
            /// 复制状态表，供复制构造函数使用。
            /// </summary>
            static Dictionary<TState, Dictionary<TTransition, TState>?> CopyStates(
                Dictionary<TState, Dictionary<TTransition, TState>?> source)
            {
                var copy = new Dictionary<TState, Dictionary<TTransition, TState>?>(
                    source.Count);

                foreach (var pair in source)
                {
                    copy.Add(
                        pair.Key,
                        pair.Value == null
                            ? null
                            : new Dictionary<TTransition, TState>(pair.Value));
                }

                return copy;
            }

            readonly Dictionary<TState, Dictionary<TTransition, TState>?> states = new();
            Dictionary<TTransition, TState>? anyTimeTransitions = new();
            readonly TState startState;
            readonly TState? defaultState;

            /// <summary>是否已经构建过状态机；构建后构造器只读</summary>
            bool built;
        }

        /// <summary>
        /// 状态接口
        /// </summary>
        public interface IState
        {
            /// <summary>
            /// 当有限状态机被构建时调用
            /// </summary>
            void OnBuild(
                FSM<TState, TStateArg, TTransition> fsm,
                TStateArg arg);

            /// <summary>
            /// 当进入该状态时调用
            /// </summary>
            /// <param name="prevState">前一个状态，如果是第一个状态则为null</param>
            /// <param name="transition">转移</param>
            void OnEnter(TState? prevState, TTransition? transition);

            /// <summary>
            /// 当退出该状态时调用
            /// </summary>
            /// <param name="transition">转移</param>
            /// <param name="nextState">下一个状态，如果是Reset前的最后一个状态则为null</param>
            void OnExit(TTransition? transition, TState? nextState);

            /// <summary>
            /// 当有限状态机被重置时调用
            /// </summary>
            /// <param name="lastState">上一个状态</param>
            /// <param name="startState">初始状态</param>
            void OnReset(TState? lastState, TState startState);
        }

        /// <summary>
        /// 进行状态转移
        /// </summary>
        /// <param name="transition">转移</param>
        /// <returns>指示状态转移是否成功</returns>
        /// <exception cref="InvalidOperationException">
        /// 当状态机正在构建、切换或重置时调用（例如在生命周期回调内）抛出。
        /// </exception>
        public bool Switch(TTransition transition)
        {
            EnsureSwitchAllowed();

            if (Switch(states[CurrentState], transition)) return true;
            if (Switch(anyTimeTransitions, transition)) return true;

            if (defaultState != null)
            {
                Switch(transition, defaultState);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 不使用 Transition 直接进行状态转移
        /// </summary>
        /// <param name="nextState">新状态</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="nextState"/> 为 null 时抛出</exception>
        /// <exception cref="ArgumentException">当 <paramref name="nextState"/> 未注册时抛出</exception>
        /// <exception cref="InvalidOperationException">
        /// 当状态机正在构建、切换或重置时调用（例如在生命周期回调内）抛出。
        /// </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Switch(TState nextState)
        {
            nextState.ThrowIfNull(nameof(nextState));
            EnsureSwitchAllowed();

            if (!states.ContainsKey(nextState))
                throw new ArgumentException(
                    "State not found in FSM.", nameof(nextState));

            Switch(new(), nextState);
        }

        /// <summary>
        /// 当前状态
        /// </summary>
        public TState CurrentState { get; private set; }

        public delegate void OnStateSwitchHandler(
            TState oldState,
            TTransition? transition,
            TState newState);


        /// <summary>
        /// 当状态转移时触发该事件
        /// 事件触发时 <see cref="CurrentState"/> 已经是新状态
        /// </summary>
        public event OnStateSwitchHandler? OnStateSwitch;

        /// <summary>
        /// 当状态机被重置时触发该事件
        /// </summary>
        public event Action? OnReset;

        /// <summary>
        /// 重置状态机
        /// 即使 OnReset 回调抛出异常，也会保证初始状态的 OnEnter 被调用
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// 当状态机正在构建、切换或重置时调用（例如在生命周期回调内）抛出。
        /// </exception>
        public void Reset()
        {
            EnsureSwitchAllowed();

            var lastState = CurrentState;
            switchAllowed = false;

            try
            {
                lastState.OnExit(new(), null);
                CurrentState = startState;

                try
                {
                    foreach (var i in states.Keys)
                        i.OnReset(lastState, startState);

                    OnReset?.Invoke();
                }
                finally
                {
                    CurrentState.OnEnter(null, new());
                }
            }
            finally
            {
                switchAllowed = true;
            }
        }

        private FSM(
            Dictionary<TState, Dictionary<TTransition, TState>?> states,
            Dictionary<TTransition, TState>? anyTimeTransitions,
            TState? defaultState,
            TState startState,
            TStateArg arg)
        {
            this.states = states;
            this.anyTimeTransitions = anyTimeTransitions;
            this.defaultState = defaultState;
            this.startState = startState;

            // 先设置 CurrentState，保证 OnBuild 期间读取 CurrentState 得到的是初始状态而不是 null
            CurrentState = startState;

            // 构建期间（OnBuild 与初始 OnEnter）关闭“可切换”闸门：
            // 回调内切换会抛异常，避免重复 OnEnter 或状态表与生命周期不一致。
            switchAllowed = false;

            try
            {
                foreach (var s in this.states.Keys)
                    s.OnBuild(this, arg);

                CurrentState.OnEnter(null, new());
            }
            finally
            {
                switchAllowed = true;
            }
        }

        bool Switch(
            Dictionary<TTransition, TState>? transitions,
            TTransition transition)
        {
            if (transitions != null
                && transitions.TryGetValue(transition, out var nextState))
            {
                Switch(transition, nextState);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 执行一次状态切换。
        /// 顺序为：旧状态 OnExit → 更新 CurrentState → 触发 OnStateSwitch → 新状态 OnEnter。
        /// OnEnter 放在 finally 中，即使事件处理器抛出异常，新状态也一定会完成进入，
        /// 避免出现"旧状态已退出、新状态未进入"的半切换状态。
        /// 切换期间关闭“可切换”闸门，回调内再调用 Switch / Reset 会抛异常。
        /// </summary>
        void Switch(TTransition? transition, TState nextState)
        {
            var oldState = CurrentState;
            switchAllowed = false;

            try
            {
                oldState.OnExit(transition, nextState);

                CurrentState = nextState;

                try
                {
                    OnStateSwitch?.Invoke(oldState, transition, nextState);
                }
                finally
                {
                    nextState.OnEnter(oldState, transition);
                }
            }
            finally
            {
                switchAllowed = true;
            }
        }

        /// <summary>
        /// 当前是否允许调用 <see cref="Switch(TTransition)"/> / <see cref="Switch(TState)"/> / <see cref="Reset"/>。
        /// 构建（含初始 OnEnter）、状态切换与重置期间为 false。
        /// </summary>
        bool switchAllowed;

        /// <summary>
        /// 校验当前处于允许切换的阶段，否则抛出 <see cref="InvalidOperationException"/>。
        /// </summary>
        void EnsureSwitchAllowed()
        {
            if (!switchAllowed)
                throw new InvalidOperationException(
                    "Switch / Reset cannot be called while the FSM is building, switching or resetting.");
        }

        readonly Dictionary<TState, Dictionary<TTransition, TState>?> states = new();
        readonly Dictionary<TTransition, TState>? anyTimeTransitions = new();
        readonly TState? defaultState;
        readonly TState startState;
    }
}
