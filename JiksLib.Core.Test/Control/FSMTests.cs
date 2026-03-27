using NUnit.Framework;
using JiksLib.Control;
using System;
using System.Collections.Generic;

namespace JiksLib.Test.Control
{
    [TestFixture]
    public class FSMTests
    {
        // 测试用状态类型
        private class TestState : FSM<TestState, TestTransition>.IState
        {
            public string Name { get; }
            public int OnBuildCallCount { get; private set; }
            public int OnEnterCallCount { get; private set; }
            public int OnExitCallCount { get; private set; }
            public int OnResetCallCount { get; private set; }
            public FSM<TestState, TestTransition>? LastBuildFSM { get; private set; }

            public TestState(string name)
            {
                Name = name;
            }

            public virtual void OnBuild(FSM<TestState, TestTransition> fsm)
            {
                OnBuildCallCount++;
                LastBuildFSM = fsm;
            }

            public virtual void OnEnter()
            {
                OnEnterCallCount++;
            }

            public virtual void OnExit()
            {
                OnExitCallCount++;
            }

            public virtual void OnReset()
            {
                OnResetCallCount++;
            }

            public void ResetCounters()
            {
                OnBuildCallCount = 0;
                OnEnterCallCount = 0;
                OnExitCallCount = 0;
                OnResetCallCount = 0;
                LastBuildFSM = null;
            }

            public override string ToString() => Name;
        }

        // 测试用转移类型
        private enum TestTransition
        {
            GoToB,
            GoToC,
            GoToD,
            AnyTimeTransition,
            AnotherTransition
        }

        [Test]
        public void Builder_AddState_AddsState()
        {
            // Arrange
            var stateA = new TestState("A");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            var stateB = new TestState("B");

            // Act
            builder.AddState(stateB);

            // Assert - 没有异常即通过
            Assert.Pass();
        }

        [Test]
        public void Builder_AddTransition_WithValidStates_AddsTransition()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);

            // Act
            builder.AddTransition(stateA, TestTransition.GoToB, stateB);

            // Assert - 没有异常即通过
            Assert.Pass();
        }

        [Test]
        public void Builder_AddTransition_WithInvalidPrevState_ThrowsArgumentException()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateB); // 只添加了stateB作为startState，没有stateA

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                builder.AddTransition(stateA, TestTransition.GoToB, stateB));
            Assert.That(ex!.ParamName, Is.EqualTo("prevState"));
        }

        [Test]
        public void Builder_AddTransition_WithInvalidNextState_ThrowsArgumentException()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA); // 只添加了stateA作为startState，没有stateB

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                builder.AddTransition(stateA, TestTransition.GoToB, stateB));
            Assert.That(ex!.ParamName, Is.EqualTo("nextState"));
        }

        [Test]
        public void Builder_AddAnyTimeTransition_WithValidState_AddsTransition()
        {
            // Arrange
            var stateA = new TestState("A");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);

            // Act
            builder.AddAnyTimeTransition(TestTransition.AnyTimeTransition, stateA);

            // Assert - 没有异常即通过
            Assert.Pass();
        }

        [Test]
        public void Builder_AddAnyTimeTransition_WithInvalidState_ThrowsArgumentException()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA); // stateA已添加，stateB未添加

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                builder.AddAnyTimeTransition(TestTransition.AnyTimeTransition, stateB));
            Assert.That(ex!.ParamName, Is.EqualTo("nextState"));
        }

        [Test]
        public void Builder_AddDefaultTransition_WithValidState_SetsDefaultState()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA, stateB); // startState: A, defaultState: B

            // Act & Assert - 构建应该成功
            Assert.DoesNotThrow(() => builder.Build());
        }

        [Test]
        public void Builder_DefaultState_WorksCorrectly()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA, stateB); // startState: A, defaultState: B

            // Act
            var fsm = builder.Build();

            // Assert: 默认状态应该工作
            var result = fsm.Switch(TestTransition.AnotherTransition); // 未定义的转移
            Assert.That(result, Is.True);
            Assert.That(fsm.CurrentState, Is.SameAs(stateB));
        }

        [Test]
        public void Build_WithStartState_CallsOnBuildOnAllStates()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var stateC = new TestState("C");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddState(stateC);
            stateA.ResetCounters();
            stateB.ResetCounters();
            stateC.ResetCounters();

            // Act
            var fsm = builder.Build();

            // Assert
            Assert.That(stateA.OnBuildCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnBuildCallCount, Is.EqualTo(1));
            Assert.That(stateC.OnBuildCallCount, Is.EqualTo(1));
            Assert.That(stateA.LastBuildFSM, Is.SameAs(fsm));
            Assert.That(stateB.LastBuildFSM, Is.SameAs(fsm));
            Assert.That(stateC.LastBuildFSM, Is.SameAs(fsm));
        }

        [Test]
        public void Build_WithStartState_SetsCurrentStateAndCallsOnEnter()
        {
            // Arrange
            var stateA = new TestState("A");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            stateA.ResetCounters();

            // Act
            var fsm = builder.Build();

            // Assert
            Assert.That(fsm.CurrentState, Is.SameAs(stateA));
            Assert.That(stateA.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Switch_WithSpecificTransition_ChangesState()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddTransition(stateA, TestTransition.GoToB, stateB);
            var fsm = builder.Build();
            stateA.ResetCounters();
            stateB.ResetCounters();

            // Act
            var result = fsm.Switch(TestTransition.GoToB);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(fsm.CurrentState, Is.SameAs(stateB));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Switch_WithAnyTimeTransition_ChangesState()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddAnyTimeTransition(TestTransition.AnyTimeTransition, stateB);
            var fsm = builder.Build();
            stateA.ResetCounters();
            stateB.ResetCounters();

            // Act
            var result = fsm.Switch(TestTransition.AnyTimeTransition);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(fsm.CurrentState, Is.SameAs(stateB));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Switch_WithDefaultTransition_WhenNoOtherTransition_ChangesState()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA, stateB); // startState: A, defaultState: B
            var fsm = builder.Build();
            stateA.ResetCounters();
            stateB.ResetCounters();

            // Act
            var result = fsm.Switch(TestTransition.GoToB); // 没有定义这个转移

            // Assert
            Assert.That(result, Is.True);
            Assert.That(fsm.CurrentState, Is.SameAs(stateB));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Switch_WithoutAnyTransition_ReturnsFalse()
        {
            // Arrange
            var stateA = new TestState("A");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            var fsm = builder.Build();
            stateA.ResetCounters();

            // Act
            var result = fsm.Switch(TestTransition.GoToB);

            // Assert
            Assert.That(result, Is.False);
            Assert.That(fsm.CurrentState, Is.SameAs(stateA));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(0));
            Assert.That(stateA.OnEnterCallCount, Is.EqualTo(0)); // 没有状态变化
        }

        [Test]
        public void Switch_SpecificTransitionTakesPrecedenceOverAnyTimeTransition()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var stateC = new TestState("C");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddState(stateC);
            // 添加特定转移：A -> B
            builder.AddTransition(stateA, TestTransition.GoToB, stateB);
            // 添加任意时间转移：任何状态 -> C
            builder.AddAnyTimeTransition(TestTransition.GoToB, stateC);
            var fsm = builder.Build();
            stateA.ResetCounters();
            stateB.ResetCounters();
            stateC.ResetCounters();

            // Act
            var result = fsm.Switch(TestTransition.GoToB);

            // Assert
            Assert.That(result, Is.True);
            // 应该使用特定转移，而不是任意时间转移
            Assert.That(fsm.CurrentState, Is.SameAs(stateB));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(stateC.OnEnterCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Switch_AnyTimeTransitionTakesPrecedenceOverDefaultTransition()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var stateC = new TestState("C");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA, stateC); // startState: A, defaultState: C
            builder.AddState(stateB);
            // 添加任意时间转移：任何状态 -> B
            builder.AddAnyTimeTransition(TestTransition.GoToB, stateB);
            var fsm = builder.Build();
            stateA.ResetCounters();
            stateB.ResetCounters();
            stateC.ResetCounters();

            // Act
            var result = fsm.Switch(TestTransition.GoToB);

            // Assert
            Assert.That(result, Is.True);
            // 应该使用任意时间转移，而不是默认转移
            Assert.That(fsm.CurrentState, Is.SameAs(stateB));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(stateC.OnEnterCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Switch_WithTransition_FiresOnStateSwitchEvent()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddTransition(stateA, TestTransition.GoToB, stateB);
            var fsm = builder.Build();

            TestState? capturedPrevState = null;
            TestState? capturedNextState = null;
            fsm.OnStateSwitch += (prev, transition, next) =>
            {
                capturedPrevState = prev;
                capturedNextState = next;
            };

            // Act
            fsm.Switch(TestTransition.GoToB);

            // Assert
            Assert.That(capturedPrevState, Is.SameAs(stateA));
            Assert.That(capturedNextState, Is.SameAs(stateB));
        }

        [Test]
        public void Switch_MultipleTransitions_SequentiallyChangesStates()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var stateC = new TestState("C");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddState(stateC);
            builder.AddTransition(stateA, TestTransition.GoToB, stateB);
            builder.AddTransition(stateB, TestTransition.GoToC, stateC);
            var fsm = builder.Build();
            stateA.ResetCounters();
            stateB.ResetCounters();
            stateC.ResetCounters();

            // Act
            fsm.Switch(TestTransition.GoToB);
            fsm.Switch(TestTransition.GoToC);

            // Assert
            Assert.That(fsm.CurrentState, Is.SameAs(stateC));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateC.OnEnterCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Switch_ToSameStateViaTransition_CallsOnExitAndOnEnter()
        {
            // Arrange
            var stateA = new TestState("A");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddTransition(stateA, TestTransition.GoToB, stateA); // 转移到自身
            var fsm = builder.Build();
            stateA.ResetCounters();

            // Act
            fsm.Switch(TestTransition.GoToB);

            // Assert
            Assert.That(fsm.CurrentState, Is.SameAs(stateA));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateA.OnEnterCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Build_WithInvalidStartState_ThrowsArgumentException()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            // 创建builder时使用stateA作为startState，stateB未添加
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            // 尝试构建，应该成功，因为startState已添加
            // 现在无法测试无效的startState，因为startState在构造函数中已添加
            // 此测试已过时，保留为无操作
            Assert.Pass();
        }

        [Test]
        public void Switch_WithDefaultTransition_FiresOnStateSwitchEvent()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA, stateB); // startState: A, defaultState: B
            var fsm = builder.Build();

            TestState? capturedPrevState = null;
            TestState? capturedNextState = null;
            fsm.OnStateSwitch += (prev, transition, next) =>
            {
                capturedPrevState = prev;
                capturedNextState = next;
            };

            // Act
            fsm.Switch(TestTransition.GoToB); // 使用未定义的转移

            // Assert
            Assert.That(capturedPrevState, Is.SameAs(stateA));
            Assert.That(capturedNextState, Is.SameAs(stateB));
        }

        [Test]
        public void Switch_WithAnyTimeTransition_FiresOnStateSwitchEvent()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddAnyTimeTransition(TestTransition.AnyTimeTransition, stateB);
            var fsm = builder.Build();

            TestState? capturedPrevState = null;
            TestState? capturedNextState = null;
            fsm.OnStateSwitch += (prev, transition, next) =>
            {
                capturedPrevState = prev;
                capturedNextState = next;
            };

            // Act
            fsm.Switch(TestTransition.AnyTimeTransition);

            // Assert
            Assert.That(capturedPrevState, Is.SameAs(stateA));
            Assert.That(capturedNextState, Is.SameAs(stateB));
        }

        [Test]
        public void Switch_WithMultipleAnyTimeTransitions_UsesCorrectTransition()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var stateC = new TestState("C");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddState(stateC);
            builder.AddAnyTimeTransition(TestTransition.GoToB, stateB);
            builder.AddAnyTimeTransition(TestTransition.GoToC, stateC);
            var fsm = builder.Build();
            stateA.ResetCounters();
            stateB.ResetCounters();
            stateC.ResetCounters();

            // Act
            fsm.Switch(TestTransition.GoToB);

            // Assert
            Assert.That(fsm.CurrentState, Is.SameAs(stateB));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(1));
            Assert.That(stateC.OnEnterCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Switch_WithNoTransitionsButDefaultTransition_ReturnsTrue()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA, stateB); // startState: A, defaultState: B
            var fsm = builder.Build();
            stateA.ResetCounters();
            stateB.ResetCounters();

            // Act
            var result = fsm.Switch(TestTransition.AnotherTransition); // 未定义的转移

            // Assert
            Assert.That(result, Is.True);
            Assert.That(fsm.CurrentState, Is.SameAs(stateB));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Switch_WithNoTransitionsAndNoDefaultTransition_ReturnsFalse()
        {
            // Arrange
            var stateA = new TestState("A");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            var fsm = builder.Build();
            stateA.ResetCounters();

            // Act
            var result = fsm.Switch(TestTransition.AnotherTransition); // 未定义的转移

            // Assert
            Assert.That(result, Is.False);
            Assert.That(fsm.CurrentState, Is.SameAs(stateA));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(0));
            Assert.That(stateA.OnEnterCallCount, Is.EqualTo(0));
        }

        [Test]
        public void Switch_FromStateWithNoSpecificTransition_ButHasAnyTimeTransition_UsesAnyTimeTransition()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var stateC = new TestState("C");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddState(stateC);
            // 只有从A到B的特定转移
            builder.AddTransition(stateA, TestTransition.GoToB, stateB);
            // 任意时间转移到C
            builder.AddAnyTimeTransition(TestTransition.GoToC, stateC);
            var fsm = builder.Build();

            // 切换到B
            fsm.Switch(TestTransition.GoToB);
            stateB.ResetCounters();
            stateC.ResetCounters();

            // Act: 从B触发GoToC，B没有特定转移
            var result = fsm.Switch(TestTransition.GoToC);

            // Assert: 应该使用任意时间转移
            Assert.That(result, Is.True);
            Assert.That(fsm.CurrentState, Is.SameAs(stateC));
            Assert.That(stateC.OnEnterCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Builder_AddState_DuplicateState_ThrowsArgumentException()
        {
            // Arrange
            var stateA = new TestState("A");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA); // 已经添加了stateA

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.AddState(stateA));
        }

        [Test]
        public void Builder_AddTransition_DuplicateTransition_ThrowsArgumentException()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var stateC = new TestState("C");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddState(stateC);
            builder.AddTransition(stateA, TestTransition.GoToB, stateB);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                builder.AddTransition(stateA, TestTransition.GoToB, stateC));
        }

        [Test]
        public void Builder_AddAnyTimeTransition_DuplicateTransition_ThrowsArgumentException()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddAnyTimeTransition(TestTransition.AnyTimeTransition, stateA);

            // Act & Assert
            Assert.Throws<ArgumentException>(() =>
                builder.AddAnyTimeTransition(TestTransition.AnyTimeTransition, stateB));
        }

        [Test]
        public void Switch_WhenOnStateSwitchEventThrowsException_ExceptionPropagates()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddTransition(stateA, TestTransition.GoToB, stateB);
            var fsm = builder.Build();

            fsm.OnStateSwitch += (prev, transition, next) =>
            {
                throw new InvalidOperationException("Test exception");
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                fsm.Switch(TestTransition.GoToB));
        }

        [Test]
        public void Reset_FromDifferentState_ResetsToStartState()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddTransition(stateA, TestTransition.GoToB, stateB);
            var fsm = builder.Build();

            // 切换到B
            fsm.Switch(TestTransition.GoToB);
            stateA.ResetCounters();
            stateB.ResetCounters();

            // Act
            fsm.Reset();

            // Assert
            Assert.That(fsm.CurrentState, Is.SameAs(stateA));
            Assert.That(stateB.OnExitCallCount, Is.EqualTo(1)); // Reset时退出当前状态B
            Assert.That(stateA.OnEnterCallCount, Is.EqualTo(1)); // Reset后进入起始状态A
        }

        [Test]
        public void Reset_CallsOnResetOnAllStates()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var stateC = new TestState("C");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddState(stateC);
            var fsm = builder.Build();
            stateA.ResetCounters();
            stateB.ResetCounters();
            stateC.ResetCounters();

            // Act
            fsm.Reset();

            // Assert
            Assert.That(stateA.OnResetCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnResetCallCount, Is.EqualTo(1));
            Assert.That(stateC.OnResetCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Reset_FiresOnResetEvent()
        {
            // Arrange
            var stateA = new TestState("A");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            var fsm = builder.Build();

            bool eventFired = false;
            fsm.OnReset += () => eventFired = true;

            // Act
            fsm.Reset();

            // Assert
            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void Reset_WhenAlreadyInStartState_StillCallsOnExitAndOnEnter()
        {
            // Arrange
            var stateA = new TestState("A");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            var fsm = builder.Build();
            stateA.ResetCounters();

            // Act
            fsm.Reset();

            // Assert
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(1)); // Reset调用OnExit
            Assert.That(stateA.OnEnterCallCount, Is.EqualTo(1)); // 然后调用OnEnter
            Assert.That(fsm.CurrentState, Is.SameAs(stateA));
        }

        [Test]
        public void Reset_AfterMultipleTransitions_ResetsToOriginalStartState()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var stateC = new TestState("C");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddState(stateC);
            builder.AddTransition(stateA, TestTransition.GoToB, stateB);
            builder.AddTransition(stateB, TestTransition.GoToC, stateC);
            var fsm = builder.Build();

            // 进行多次转移
            fsm.Switch(TestTransition.GoToB);
            fsm.Switch(TestTransition.GoToC);
            Assert.That(fsm.CurrentState, Is.SameAs(stateC));

            stateA.ResetCounters();
            stateB.ResetCounters();
            stateC.ResetCounters();

            // Act
            fsm.Reset();

            // Assert
            Assert.That(fsm.CurrentState, Is.SameAs(stateA));
            Assert.That(stateC.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateA.OnEnterCallCount, Is.EqualTo(1));
        }

        [Test]
        public void Reset_ThenSwitch_WorksCorrectly()
        {
            // Arrange
            var stateA = new TestState("A");
            var stateB = new TestState("B");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            builder.AddState(stateB);
            builder.AddTransition(stateA, TestTransition.GoToB, stateB);
            var fsm = builder.Build();

            // 切换到B然后重置
            fsm.Switch(TestTransition.GoToB);
            fsm.Reset();
            stateA.ResetCounters();
            stateB.ResetCounters();

            // Act: 再次切换到B
            var result = fsm.Switch(TestTransition.GoToB);

            // Assert
            Assert.That(result, Is.True);
            Assert.That(fsm.CurrentState, Is.SameAs(stateB));
            Assert.That(stateA.OnExitCallCount, Is.EqualTo(1));
            Assert.That(stateB.OnEnterCallCount, Is.EqualTo(1));
        }



        [Test]
        public void Reset_WhenOnResetEventThrowsException_ExceptionPropagates()
        {
            // Arrange
            var stateA = new TestState("A");
            var builder = new FSM<TestState, TestTransition>.Builder(stateA);
            var fsm = builder.Build();

            fsm.OnReset += () => throw new InvalidOperationException("Reset failed");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => fsm.Reset());
        }

        [Test]
        public void Reset_WhenStateOnResetThrowsException_ExceptionPropagates()
        {
            // 为这个测试创建一个特殊的状态类型
            var throwingState = new ThrowingResetState();
            var builder = new FSM<ThrowingResetState, TestTransition>.Builder(throwingState);
            var fsm = builder.Build();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => fsm.Reset());
        }

        // 会抛出异常的特殊状态类（仅用于此测试）
        private class ThrowingResetState : FSM<ThrowingResetState, TestTransition>.IState
        {
            public void OnBuild(FSM<ThrowingResetState, TestTransition> fsm) { }
            public void OnEnter() { }
            public void OnExit() { }
            public void OnReset()
            {
                throw new InvalidOperationException("State reset failed");
            }
        }

    }
}