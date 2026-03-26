namespace JiksLib.Control
{
    /// <summary>
    /// 轻量级有限状态机
    /// </summary>
    /// <typeparam name="TState">状态类型</typeparam>
    public sealed class LightFSM<TState>
        where TState : LightFSM<TState>.IState
    {
        /// <summary>
        /// 构造轻量级有限状态机
        /// </summary>
        /// <param name="startState">初始状态，将会立刻调用 OnEnter</param>
        public LightFSM(TState startState)
        {
            CurrentState = startState;
            startState.OnEnter(this);
        }

        /// <summary>
        /// 切换状态
        /// </summary>
        /// <param name="nextState">新状态</param>
        public void Switch(TState nextState)
        {
            CurrentState.OnExit();
            OnStateSwitch?.Invoke(CurrentState, nextState);
            CurrentState = nextState;
            CurrentState.OnEnter(this);
        }

        /// <summary>
        /// 当前状态
        /// </summary>
        public TState CurrentState { get; private set; }

        public delegate void OnStateSwitchHandler(
            TState prevState,
            TState nextState);

        /// <summary>
        /// 当状态切换时触发该事件
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
            void OnEnter(LightFSM<TState> fsm);

            /// <summary>
            /// 当退出该状态时
            /// </summary>
            void OnExit();
        }
    }
}
