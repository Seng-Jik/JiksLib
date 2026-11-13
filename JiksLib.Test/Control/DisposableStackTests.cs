using System;
using System.Collections.Generic;
using NUnit.Framework;
using JiksLib.Control;

namespace JiksLib.Test.Control
{
    [TestFixture]
    public class DisposableStackTests
    {
        sealed class CountingDisposable : IDisposable
        {
            public int DisposeCount { get; private set; }

            public void Dispose() => DisposeCount++;
        }

        sealed class ThrowingDisposable : IDisposable
        {
            public ThrowingDisposable(string message) => this.message = message;

            public void Dispose() => throw new InvalidOperationException(message);

            readonly string message;
        }

        [Test]
        public void NewStack_Dispose_DoesNotThrow()
        {
            var stack = new DisposableStack();

            Assert.That(() => stack.Dispose(), Throws.Nothing);
        }

        [Test]
        public void AddDisposable_Dispose_DisposesAddedObject()
        {
            var disposable = new CountingDisposable();
            var stack = new DisposableStack();
            stack.Add(disposable);

            stack.Dispose();

            Assert.That(disposable.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void AddAction_Dispose_InvokesAddedAction()
        {
            var count = 0;
            var stack = new DisposableStack();
            stack.Add(() => count++);

            stack.Dispose();

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_DisposesInReverseOrderOfAddition()
        {
            var order = new List<int>();
            var stack = new DisposableStack();
            stack.Add(() => order.Add(1));
            stack.Add(() => order.Add(2));
            stack.Add(() => order.Add(3));

            stack.Dispose();

            Assert.That(order, Is.EqualTo(new[] { 3, 2, 1 }));
        }

        [Test]
        public void Dispose_MixedDisposablesAndActions_DisposesInReverseOrder()
        {
            var order = new List<string>();
            var first = new RecordingDisposable("disposable1", order);
            var third = new RecordingDisposable("disposable3", order);
            var stack = new DisposableStack();
            stack.Add(first);
            stack.Add(() => order.Add("action2"));
            stack.Add(third);

            stack.Dispose();

            Assert.That(order, Is.EqualTo(
                new[] { "disposable3", "action2", "disposable1" }));
        }

        [Test]
        public void Dispose_CalledTwice_DisposesEachObjectOnlyOnce()
        {
            var disposable = new CountingDisposable();
            var actionCount = 0;
            var stack = new DisposableStack();
            stack.Add(disposable);
            stack.Add(() => actionCount++);

            stack.Dispose();
            stack.Dispose();

            Assert.That(disposable.DisposeCount, Is.EqualTo(1));
            Assert.That(actionCount, Is.EqualTo(1));
        }

        [Test]
        public void AddDisposable_Null_ThrowsArgumentNullException()
        {
            var stack = new DisposableStack();

            Assert.That(
                () => stack.Add((IDisposable)null!),
                Throws.ArgumentNullException);
        }

        [Test]
        public void AddAction_Null_ThrowsArgumentNullException()
        {
            var stack = new DisposableStack();

            Assert.That(
                () => stack.Add((Action)null!),
                Throws.ArgumentNullException);
        }

        [Test]
        public void Add_AfterDispose_StackCanBeReused()
        {
            var firstCount = 0;
            var secondCount = 0;
            var stack = new DisposableStack();
            stack.Add(() => firstCount++);
            stack.Dispose();

            stack.Add(() => secondCount++);

            Assert.That(firstCount, Is.EqualTo(1));
            Assert.That(secondCount, Is.EqualTo(0));

            stack.Dispose();
            Assert.That(secondCount, Is.EqualTo(1));
        }

        [Test]
        public void AsDisposable_ReturnsSelf()
        {
            var stack = new DisposableStack();

            Assert.That(stack.AsDisposable(), Is.SameAs(stack));
        }

        [Test]
        public void AsDisposable_UsedInUsing_DisposesStackOnExit()
        {
            var disposable = new CountingDisposable();
            using (var stack = new DisposableStack())
            {
                stack.Add(disposable);
                Assert.That(disposable.DisposeCount, Is.EqualTo(0));
            }

            Assert.That(disposable.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void CapacityConstructor_CreatesUsableStack()
        {
            var count = 0;
            var stack = new DisposableStack(16);
            stack.Add(() => count++);

            stack.Dispose();

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Constructor_NegativeCapacity_ThrowsArgumentOutOfRangeException()
        {
            Assert.That(
                () => new DisposableStack(-1),
                Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void Dispose_OneObjectThrows_StillDisposesRemainingObjects()
        {
            var first = new CountingDisposable();
            var last = new CountingDisposable();
            var stack = new DisposableStack();
            stack.Add(first);
            stack.Add(new ThrowingDisposable("boom"));
            stack.Add(last);

            Assert.That(() => stack.Dispose(), Throws.InvalidOperationException);

            Assert.That(first.DisposeCount, Is.EqualTo(1));
            Assert.That(last.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void Dispose_SingleException_RethrowsOriginalException()
        {
            var stack = new DisposableStack();
            stack.Add(new ThrowingDisposable("boom"));

            var exception = Assert.Throws<InvalidOperationException>(
                () => stack.Dispose());

            Assert.That(exception!.Message, Is.EqualTo("boom"));
        }

        [Test]
        public void Dispose_MultipleExceptions_ThrowsAggregateException()
        {
            var stack = new DisposableStack();
            stack.Add(new ThrowingDisposable("first"));
            stack.Add(new ThrowingDisposable("second"));

            var exception = Assert.Throws<AggregateException>(
                () => stack.Dispose());

            Assert.That(exception!.InnerExceptions, Has.Count.EqualTo(2));
        }

        [Test]
        public void Dispose_AfterException_DoesNotRetryRemainingObjects()
        {
            var disposable = new CountingDisposable();
            var stack = new DisposableStack();
            stack.Add(disposable);
            stack.Add(new ThrowingDisposable("boom"));

            Assert.That(() => stack.Dispose(), Throws.InvalidOperationException);
            Assert.That(() => stack.Dispose(), Throws.Nothing);

            Assert.That(disposable.DisposeCount, Is.EqualTo(1));
        }

        sealed class RecordingDisposable : IDisposable
        {
            public RecordingDisposable(string name, List<string> order)
            {
                this.name = name;
                this.order = order;
            }

            public void Dispose() => order.Add(name);

            readonly string name;
            readonly List<string> order;
        }
    }
}
