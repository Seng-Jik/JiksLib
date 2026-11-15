using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 有限状态机
    /// </summary>
    public sealed class FSM<TState, TStateArg, TTransition>
        where TState : class, FSM<TState, TStateArg, TTransition>.IState
        where TTransition : struct
    {
        /// <summary>
        /// 构造器
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
            /// 添加状态
            /// 重复添加同一状态是幂等的
            /// </summary>
            /// <param name="state">状态对象</param>
            public void AddState(TState state)
            {
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
            public void AddTransition(
                TState prevState,
                TTransition transition,
                TState nextState)
            {
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
            public void AddAnyTimeTransition(
                TTransition transition,
                TState nextState)
            {
                if (!states.ContainsKey(nextState))
                    throw new ArgumentException(
                        "State not found, call AddState first.", nameof(nextState));

                (anyTimeTransitions ??= new()).Add(transition, nextState);
            }

            /// <summary>
            /// 构建有限状态机
            /// 构建时会复制状态表，因此之后再调用 <see cref="AddState"/> /
            /// <see cref="AddTransition"/> / <see cref="AddAnyTimeTransition"/>
            /// 不会影响已经构建的状态机
            /// </summary>
            public FSM<TState, TStateArg, TTransition> Build(TStateArg arg) =>
                new(
                    CopyStates(states),
                    anyTimeTransitions == null
                        ? null
                        : new Dictionary<TTransition, TState>(anyTimeTransitions),
                    defaultState,
                    startState,
                    arg);

            /// <summary>
            /// 复制状态表，避免构建后继续修改 Builder 影响到已构建的状态机。
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
        public bool Switch(TTransition transition)
        {
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Switch(TState nextState)
        {
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
        public void Reset()
        {
            var lastState = CurrentState;
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

            foreach (var s in this.states.Keys)
                s.OnBuild(this, arg);

            CurrentState.OnEnter(null, new());
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
        /// </summary>
        void Switch(TTransition? transition, TState nextState)
        {
            var oldState = CurrentState;

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

        readonly Dictionary<TState, Dictionary<TTransition, TState>?> states = new();
        readonly Dictionary<TTransition, TState>? anyTimeTransitions = new();
        readonly TState? defaultState;
        readonly TState startState;
    }
}
