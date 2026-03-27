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
            public string Name { get; }

            public TestState(string name)
            {
                Name = name;
            }

            public void OnEnter(LightFSM<TestState> fsm)
            {
                OnEnterCallCount++;
                LastEnterFSM = fsm;
            }

            public void OnExit(LightFSM<TestState> fsm)
            {
                OnExitCallCount++;
            }

            public void ResetCounters()
            {
                OnEnterCallCount = 0;
                OnExitCallCount = 0;
                LastEnterFSM = null;
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
        public void Switch_EventThrowsException_DoesNotUpdateState()
        {
            // Arrange
            var state1 = new TestState("State1");
            var state2 = new TestState("State2");
            var fsm = new LightFSM<TestState>(state1);
            fsm.OnStateSwitch += (prev, next) => throw new InvalidOperationException("Test exception");
            state1.ResetCounters();
            state2.ResetCounters();

            // Act & Assert
            // Exception should propagate, but state should still be updated
            // According to the code: OnExit -> OnStateSwitch -> set CurrentState -> OnEnter
            // If event throws, the exception propagates and CurrentState may not be updated?
            // Let's examine the Switch method:
            // CurrentState.OnExit();
            // OnStateSwitch?.Invoke(CurrentState, nextState);
            // CurrentState = nextState;
            // CurrentState.OnEnter(this);
            // If Invoke throws, CurrentState assignment and OnEnter will not execute.
            // So we need to test that exception is thrown and state is not updated.
            // Actually, we should test the actual behavior.
            Assert.Throws<InvalidOperationException>(() => fsm.Switch(state2));
            Assert.That(fsm.CurrentState, Is.SameAs(state1));
            Assert.That(state1.OnExitCallCount, Is.EqualTo(1));
            Assert.That(state2.OnEnterCallCount, Is.EqualTo(0));
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
        public void Switch_NullState_ThrowsNullReferenceException()
        {
            // Arrange
            var state1 = new TestState("State1");
            var fsm = new LightFSM<TestState>(state1);

            // Act & Assert
            Assert.Throws<NullReferenceException>(() => fsm.Switch(null!));
        }

        [Test]
        public void Constructor_NullStartState_ThrowsNullReferenceException()
        {
            // Arrange & Act & Assert
            Assert.Throws<NullReferenceException>(() => new LightFSM<TestState>(null!));
        }
    }
}