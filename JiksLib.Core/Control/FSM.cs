using System;
using System.Collections.Generic;

namespace JiksLib.Control
{
    /// <summary>
    /// 有限状态机
    /// </summary>
    public sealed class FSM<TState, TTransition>
        where TState : notnull, FSM<TState, TTransition>.IState
        where TTransition : notnull
    {
        /// <summary>
        /// 构造器
        /// </summary>
        public sealed class Builder
        {
            /// <summary>
            /// 添加状态
            /// </summary>
            /// <param name="state">状态对象</param>
            public void AddState(TState state) =>
                states.Add(state, null);

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
            /// 添加默认转移
            /// 当无法找到要转移到的目标状态时，转移到此状态
            /// </summary>
            public void AddDefaultTransition(TState defaultState)
            {
                if (this.defaultState != null)
                    throw new InvalidOperationException(
                        "Default transition already added.");
    
                if (!states.ContainsKey(defaultState))
                    throw new ArgumentException(
                        "State not found, call AddState first.", nameof(nextState));

                this.defaultState = defaultState;
            }

            /// <summary>
            /// 构建有限状态机
            /// </summary>
            public FSM<TState, TTransition> Build(TState startState)
            {
                if (!states.ContainsKey(startState))
                    throw new ArgumentException(
                        "Invalid start state.", nameof(startState));

                return new(states, anyTimeTransitions, defaultState, startState);
            }

            readonly Dictionary<TState, Dictionary<TTransition, TState>?> states = new();
            Dictionary<TTransition, TState>? anyTimeTransitions = new();
            TState? defaultState;
        }

        /// <summary>
        /// 状态接口
        /// </summary>
        public interface IState
        {
            /// <summary>
            /// 当有限状态机被构建时调用
            /// </summary>
            void OnBuild(FSM<TState, TTransition> fsm);

            /// <summary>
            /// 当进入该状态时调用
            /// </summary>
            void OnEnter();

            /// <summary>
            /// 当退出该状态时调用
            /// </summary>
            void OnExit();

            /// <summary>
            /// 当有限状态机被重置时调用
            /// </summary>
            void OnReset();
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
        /// 当前状态
        /// </summary>
        public TState CurrentState { get; private set; }

        public delegate void OnStateSwitchHandler(
            TState prevState,
            TTransition transition,
            TState nextState);

        /// <summary>
        /// 当状态转移时触发该事件
        /// </summary>
        public event OnStateSwitchHandler? OnStateSwitch;

        /// <summary>
        /// 当状态机被重置时触发该事件
        /// </summary>
        public event Action? OnReset;

        /// <summary>
        /// 重置状态机
        /// </summary>
        public void Reset()
        {
            CurrentState.OnExit();
            CurrentState = startState;

            foreach (var i in states.Keys)
                i.OnReset();

            OnReset?.Invoke();

            CurrentState.OnEnter();
        }

        private FSM(
            Dictionary<TState, Dictionary<TTransition, TState>?> states,
            Dictionary<TTransition, TState>? anyTimeTransitions,
            TState? defaultState,
            TState startState)
        {
            this.states = states;
            this.anyTimeTransitions = anyTimeTransitions;
            this.defaultState = defaultState;
            this.startState = startState;

            foreach (var s in this.states.Keys)
                s.OnBuild(this);

            CurrentState = startState;
            CurrentState.OnEnter();
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

        void Switch(TTransition transition, TState nextState)
        {
            CurrentState.OnExit();
            OnStateSwitch?.Invoke(CurrentState, transition, nextState);
            CurrentState = nextState;
            CurrentState.OnEnter();
        }

        readonly Dictionary<TState, Dictionary<TTransition, TState>?> states = new();
        readonly Dictionary<TTransition, TState>? anyTimeTransitions = new();
        readonly TState? defaultState;
        readonly TState startState;
    }
}
