using NUnit.Framework;
using JiksLib.Control;
using System;

namespace JiksLib.Test.Control
{
    [TestFixture]
    public class LightFSMTests
    {
        private class TestState : LightFSM<TestState>.IState
        {
            public int OnEnterCallCount { get; private set; }
            public int OnExitCallCount { get; private set; }
            public LightFSM<TestState>? LastEnterFSM { get; private set; }
            public TestState? LastEnterPrevState { get; private set; }
            public TestState? LastExitNextState { get; private set; }
            public string Name { get; }

            public TestState(string name)
            {
                Name = name;
            }

            public virtual void OnEnter(LightFSM<TestState> fsm, TestState? prevState)
            {
                OnEnterCallCount++;
                LastEnterFSM = fsm;
                LastEnterPrevState = prevState;
            }

            public virtual void OnExit(LightFSM<TestState> fsm, TestState nextState)
            {
                OnExitCallCount++;
                LastExitNextState = nextState;
            }

            public void ResetCounters()
            {
                OnEnterCallCount = 0;
                OnExitCallCount = 0;
                LastEnterFSM = null;
                LastEnterPrevState = null;
                LastExitNextState = null;
            }
        }

        // 可在回调中执行任意动作的状态，用于验证“切换期间禁止再次 Switch”
        private sealed class ActionState : TestState
        {
            public ActionState(string name) : base(name) { }

            public Action<LightFSM<TestState>, TestState?>? EnterAction { get; set; }

            public Action<LightFSM<TestState>, TestState>? ExitAction { get; set; }

            public override void OnEnter(LightFSM<TestState> fsm, TestState? prevState)
            {
                base.OnEnter(fsm, prevState);
                EnterAction?.Invoke(fsm, prevState);
            }

            public override void OnExit(LightFSM<TestState> fsm, TestState nextState)
            {
                base.OnExit(fsm, nextState);
                ExitAction?.Invoke(fsm, nextState);
            }
        }

        [Test]
        public void Constructor_WithStartState_SetsCurrentStateAndCallsOnEnter()
        {
            // Arrange
            var startState = new TestState("Start");

            // Act
            var fsm = new LightFSM<TestState>(startState);

            // Assert
            Assert.That(fsm.CurrentState, Is.SameAs(startState));
            Assert.That(startState.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(startState.LastEnterFSM, Is.SameAs(fsm));
        }

        [Test]
        public void Switch_ToNewState_CallsOnExitOnCurrentStateAndOnEnterOnNextState()
        {
            // Arrange
            var state1 = new TestState("State1");
            var state2 = new TestState("State2");
            var fsm = new LightFSM<TestState>(state1);
            state1.ResetCounters();
            state2.ResetCounters();

            // Act
            fsm.Switch(state2);

            // Assert
            Assert.That(state1.OnExitCallCount, Is.EqualTo(1));
            Assert.That(state2.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(state2.LastEnterFSM, Is.SameAs(fsm));
            Assert.That(fsm.CurrentState, Is.SameAs(state2));
        }

        [Test]
        public void Switch_ToNewState_FiresOnStateSwitchEvent()
        {
            // Arrange
            var state1 = new TestState("State1");
            var state2 = new TestState("State2");
            var fsm = new LightFSM<TestState>(state1);
            TestState? prevStateCaptured = null;
            TestState? nextStateCaptured = null;
            fsm.OnStateSwitch += (prev, next) =>
            {
                prevStateCaptured = prev;
                nextStateCaptured = next;
            };

            // Act
            fsm.Switch(state2);

            // Assert
            Assert.That(prevStateCaptured, Is.SameAs(state1));
            Assert.That(nextStateCaptured, Is.SameAs(state2));
        }

        [Test]
        public void Switch_MultipleTimes_SequentiallyCallsOnExitAndOnEnter()
        {
            // Arrange
            var state1 = new TestState("State1");
            var state2 = new TestState("State2");
            var state3 = new TestState("State3");
            var fsm = new LightFSM<TestState>(state1);
            // state1 already has OnEnterCallCount = 1 from constructor
            state2.ResetCounters();
            state3.ResetCounters();

            // Act
            fsm.Switch(state2);
            fsm.Switch(state3);

            // Assert
            Assert.That(state1.OnExitCallCount, Is.EqualTo(1));
            Assert.That(state1.OnEnterCallCount, Is.EqualTo(1)); // Only from initial construction

            Assert.That(state2.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(state2.OnExitCallCount, Is.EqualTo(1));

            Assert.That(state3.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(state3.OnExitCallCount, Is.EqualTo(0)); // Not exited yet
            Assert.That(fsm.CurrentState, Is.SameAs(state3));
        }

        [Test]
        public void Switch_ToSameState_CallsOnExitAndOnEnter()
        {
            // Arrange
            var state = new TestState("State");
            var fsm = new LightFSM<TestState>(state);
            state.ResetCounters();

            // Act
            fsm.Switch(state);

            // Assert
            Assert.That(state.OnExitCallCount, Is.EqualTo(1));
            Assert.That(state.OnEnterCallCount, Is.EqualTo(1)); // One from constructor, one from switch
            // Total OnEnter calls: 2 (constructor + switch)
            // But we reset after constructor, so OnEnterCallCount should be 1 after switch
            // Wait, we reset counters after constructor, so OnEnterCallCount was 0 before switch.
            // Switch will call OnExit then OnEnter, so OnEnterCallCount becomes 1.
            // That's correct.
            Assert.That(fsm.CurrentState, Is.SameAs(state));
        }

        [Test]
        public void OnStateSwitch_EventUnsubscribed_DoesNotFire()
        {
            // Arrange
            var state1 = new TestState("State1");
            var state2 = new TestState("State2");
            var fsm = new LightFSM<TestState>(state1);
            int eventCallCount = 0;
            LightFSM<TestState>.OnStateSwitchHandler handler = (prev, next) => eventCallCount++;
            fsm.OnStateSwitch += handler;
            fsm.OnStateSwitch -= handler; // Unsubscribe

            // Act
            fsm.Switch(state2);

            // Assert
            Assert.That(eventCallCount, Is.EqualTo(0));
        }

        [Test]
        public void OnStateSwitch_MultipleSubscribers_AllFire()
        {
            // Arrange
            var state1 = new TestState("State1");
            var state2 = new TestState("State2");
            var fsm = new LightFSM<TestState>(state1);
            int callCount1 = 0;
            int callCount2 = 0;
            fsm.OnStateSwitch += (prev, next) => callCount1++;
            fsm.OnStateSwitch += (prev, next) => callCount2++;

            // Act
            fsm.Switch(state2);

            // Assert
            Assert.That(callCount1, Is.EqualTo(1));
            Assert.That(callCount2, Is.EqualTo(1));
        }

        [Test]
        public void Switch_EventThrowsException_StillUpdatesStateAndEntersNewState()
        {
            // Arrange
            var state1 = new TestState("State1");
            var state2 = new TestState("State2");
            var fsm = new LightFSM<TestState>(state1);
            fsm.OnStateSwitch += (prev, next) => throw new InvalidOperationException("Test exception");
            state1.ResetCounters();
            state2.ResetCounters();

            // Act & Assert
            // 与 FSM 一致：先更新 CurrentState 再触发事件；事件异常向外传播，
            // 但新状态的 OnEnter 在 finally 中执行，不会出现“旧状态已退出、新状态未进入”。
            Assert.Throws<InvalidOperationException>(() => fsm.Switch(state2));
            Assert.That(fsm.CurrentState, Is.SameAs(state2));
            Assert.That(state1.OnExitCallCount, Is.EqualTo(1));
            Assert.That(state2.OnEnterCallCount, Is.EqualTo(1));
        }


        [Test]
        public void CurrentState_Initially_SetToStartState()
        {
            // Arrange
            var startState = new TestState("Start");

            // Act
            var fsm = new LightFSM<TestState>(startState);

            // Assert
            Assert.That(fsm.CurrentState, Is.SameAs(startState));
        }

        [Test]
        public void OnStateSwitch_EventCanBeNull_NoException()
        {
            // Arrange
            var state1 = new TestState("State1");
            var state2 = new TestState("State2");
            var fsm = new LightFSM<TestState>(state1);
            // OnStateSwitch is initially null, no subscribers

            // Act & Assert
            Assert.DoesNotThrow(() => fsm.Switch(state2));
        }

        [Test]
        public void Switch_NullState_ThrowsArgumentNullException()
        {
            // Arrange
            var state1 = new TestState("State1");
            var fsm = new LightFSM<TestState>(state1);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => fsm.Switch(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("nextState"));
        }

        [Test]
        public void Constructor_NullStartState_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(
                () => new LightFSM<TestState>(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("startState"));
        }

        [Test]
        public void OnStateSwitch_EventSeesNewCurrentState()
        {
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var fsm = new LightFSM<TestState>(stateA);
            TestState? seen = null;

            fsm.OnStateSwitch += (oldState, newState) => seen = fsm.CurrentState;

            fsm.Switch(stateB);

            Assert.That(seen, Is.SameAs(stateB));
        }

        [Test]
        public void Switch_FromOnStateSwitchHandler_ThrowsAndCompletesTransition()
        {
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var stateC = new TestState("C");
            var fsm = new LightFSM<TestState>(stateA);
            stateA.ResetCounters();

            fsm.OnStateSwitch += (oldState, newState) =>
            {
                if (newState == stateB)
                    fsm.Switch(stateC);
            };

            Assert.Throws<InvalidOperationException>(() => fsm.Switch(stateB));

            Assert.That(fsm.CurrentState, Is.SameAs(stateB));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(stateC.OnEnterCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Switch_FromOnEnter_ThrowsAndKeepsNewState()
        {
            var stateA = new TestState("A");
            var stateC = new TestState("C");
            var stateB = new ActionState("B");
            var fsm = new LightFSM<TestState>(stateA);
            stateA.ResetCounters();
            stateB.EnterAction = (inner, _) => inner.Switch(stateC);

            Assert.Throws<InvalidOperationException>(() => fsm.Switch(stateB));

            Assert.That(fsm.CurrentState, Is.SameAs(stateB));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(stateC.OnEnterCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Switch_FromOnExit_ThrowsAndKeepsOldState()
        {
            var stateB = new TestState("B");
            var stateA = new ActionState("A");
            var fsm = new LightFSM<TestState>(stateA);
            stateA.ResetCounters();
            stateA.ExitAction = (inner, next) => inner.Switch(stateB);

            Assert.Throws<InvalidOperationException>(() => fsm.Switch(stateB));

            Assert.That(fsm.CurrentState, Is.SameAs(stateA));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WhenOnEnterSwitches_Throws()
        {
            var other = new TestState("Other");
            var start = new ActionState("Start");
            start.EnterAction = (inner, _) => inner.Switch(other);

            Assert.Throws<InvalidOperationException>(
                () => new LightFSM<TestState>(start));
        }
    }
}
