using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using JiksLib.Control;

namespace JiksLib.Test.Control
{
    [TestFixture]
    public class DisposableTests
    {
        [Test]
        public void Empty_ReturnsNonNullInstance()
        {
            Assert.That(Disposable.Empty, Is.Not.Null);
        }

        [Test]
        public void Empty_ReturnsSameInstance()
        {
            var first = Disposable.Empty;
            var second = Disposable.Empty;

            Assert.That(first, Is.SameAs(second));
        }

        [Test]
        public void Empty_RepeatedAccess_DoesNotAllocate()
        {
            // 先访问一次，确保类型初始化（唯一一次分配）已经完成
            _ = Disposable.Empty;

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
                _ = Disposable.Empty;
            var after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void Empty_Dispose_DoesNotThrow()
        {
            var empty = Disposable.Empty;

            Assert.That(() => empty.Dispose(), Throws.Nothing);
        }

        [Test]
        public void Empty_DisposeRepeatedly_DoesNotThrow()
        {
            var empty = Disposable.Empty;

            empty.Dispose();

            Assert.That(() => empty.Dispose(), Throws.Nothing);
        }

        [Test]
        public void FromAction_ReturnsNonNullInstance()
        {
            Assert.That(Disposable.FromAction(() => { }), Is.Not.Null);
        }

        [Test]
        public void FromAction_NullAction_ThrowsArgumentNullException()
        {
            Assert.That(
                () => Disposable.FromAction(null!),
                Throws.ArgumentNullException);
        }

        [Test]
        public void FromAction_DoesNotInvokeActionBeforeDispose()
        {
            var invoked = false;

            _ = Disposable.FromAction(() => invoked = true);

            Assert.That(invoked, Is.False);
        }

        [Test]
        public void FromAction_Dispose_InvokesAction()
        {
            var count = 0;
            var disposable = Disposable.FromAction(() => count++);

            disposable.Dispose();

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void FromAction_DisposeTwice_InvokesActionOnlyOnce()
        {
            var count = 0;
            var disposable = Disposable.FromAction(() => count++);

            disposable.Dispose();
            disposable.Dispose();

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void FromAction_MultipleInstances_AreIndependent()
        {
            var first = 0;
            var second = 0;
            var firstDisposable = Disposable.FromAction(() => first++);
            var secondDisposable = Disposable.FromAction(() => second++);

            firstDisposable.Dispose();
            Assert.That((first, second), Is.EqualTo((1, 0)));

            secondDisposable.Dispose();
            Assert.That((first, second), Is.EqualTo((1, 1)));
        }

        [Test]
        public void FromAction_ConcurrentDispose_InvokesActionOnlyOnce()
        {
            var count = 0;
            var disposable = Disposable.FromAction(() => Interlocked.Increment(ref count));

            Parallel.For(0, 100, _ => disposable.Dispose());

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void FromAction_ActionThrows_ExceptionIsPropagated()
        {
            var disposable = Disposable.FromAction(
                () => throw new InvalidOperationException("boom"));

            var exception = Assert.Throws<InvalidOperationException>(
                () => disposable.Dispose());

            Assert.That(exception!.Message, Is.EqualTo("boom"));
        }

        [Test]
        public void FromAction_ActionThrows_SubsequentDisposeDoesNotInvokeAgain()
        {
            var count = 0;
            var disposable = Disposable.FromAction(() =>
            {
                count++;
                throw new InvalidOperationException("boom");
            });

            Assert.That(() => disposable.Dispose(), Throws.InvalidOperationException);
            Assert.That(() => disposable.Dispose(), Throws.Nothing);

            Assert.That(count, Is.EqualTo(1));
        }
    }
}
