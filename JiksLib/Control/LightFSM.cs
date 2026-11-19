using System;
using JiksLib.Extensions;

namespace JiksLib.Control
{
    /// <summary>
    /// 轻量级有限状态机
    /// </summary>
    /// <remarks>
    /// 状态切换顺序与 <see cref="FSM{TState,TStateArg,TTransition}"/> 一致：
    /// 旧状态 OnExit → 更新 <see cref="CurrentState"/> → 触发 OnStateSwitch → 新状态 OnEnter。
    /// 构建（初始 OnEnter）与切换期间关闭“可切换”闸门，回调内再次调用 <see cref="Switch"/>
    /// 会抛出 <see cref="InvalidOperationException"/>；事件处理器抛出异常时，
    /// 状态仍会完成切换（新状态 OnEnter 在 finally 中执行）。
    /// </remarks>
    /// <typeparam name="TState">状态类型</typeparam>
    public sealed class LightFSM<TState>
        where TState : class, LightFSM<TState>.IState
    {
        /// <summary>
        /// 构造轻量级有限状态机
        /// </summary>
        /// <param name="startState">初始状态，将会立刻调用 OnEnter</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="startState"/> 为 null 时抛出</exception>
        /// <exception cref="InvalidOperationException">
        /// 初始 OnEnter 内调用 <see cref="Switch"/> 时抛出（构建期间不允许切换）
        /// </exception>
        public LightFSM(TState startState)
        {
            CurrentState = startState.ThrowIfNull(nameof(startState));

            try
            {
                CurrentState.OnEnter(this, null);
            }
            finally
            {
                switchAllowed = true;
            }
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="nextState">新状态</param>
        /// <exception cref="ArgumentNullException">当 <paramref name="nextState"/> 为 null 时抛出</exception>
        /// <exception cref="InvalidOperationException">
        /// 当状态机正在切换或构建时调用（例如在生命周期回调内）抛出。
        /// </exception>
        public void Switch(TState nextState)
        {
            nextState.ThrowIfNull(nameof(nextState));
            EnsureSwitchAllowed();

            var oldState = CurrentState;
            switchAllowed = false;

            try
            {
                oldState.OnExit(this, nextState);

                CurrentState = nextState;

                try
                {
                    OnStateSwitch?.Invoke(oldState, nextState);
                }
                finally
                {
                    nextState.OnEnter(this, oldState);
                }
            }
            finally
            {
                switchAllowed = true;
            }
        }

        /// <summary>
        /// 当前状态
        /// </summary>
        public TState CurrentState { get; private set; }

        public delegate void OnStateSwitchHandler(
            TState oldState,
            TState newState);

        /// <summary>
        /// 当状态切换时触发该事件
        /// 事件触发时 <see cref="CurrentState"/> 已经是新状态
        /// </summary>
        public event OnStateSwitchHandler? OnStateSwitch;

        /// <summary>
        /// 轻量级状态机的状态需要实现该接口
        /// </summary>
        public interface IState
        {
            /// <summary>
            /// 当进入该状态时
            /// </summary>
            void OnEnter(LightFSM<TState> fsm, TState? prevState);

            /// <summary>
            /// 当退出该状态时
            /// </summary>
            void OnExit(LightFSM<TState> fsm, TState nextState);
        }

        /// <summary>
        /// 当前是否允许调用 <see cref="Switch"/>；构建（初始 OnEnter）与切换期间为 false
        /// </summary>
        bool switchAllowed;

        /// <summary>
        /// 校验当前处于允许切换的阶段，否则抛出 <see cref="InvalidOperationException"/>
        /// </summary>
        void EnsureSwitchAllowed()
        {
            if (!switchAllowed)
                throw new InvalidOperationException(
                    "Switch cannot be called while the LightFSM is building or switching.");
        }
    }
}
