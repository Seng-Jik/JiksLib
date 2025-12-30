using NUnit.Framework;
using JiksLib.Control;
using System;
using System.Collections.Generic;

namespace JiksLib.Test.Control
{
    [TestFixture]
    public class DisposableTests
    {
        [Test]
        public void Null_IsNotNull()
        {
            // Arrange & Act & Assert
            Assert.That(Disposable.Null, Is.Not.Null);
        }

        [Test]
        public void Null_Dispose_DoesNotThrow()
        {
            // Arrange
            var disposable = Disposable.Null;

            // Act & Assert
            Assert.DoesNotThrow(() => disposable.Dispose());
        }

        [Test]
        public void FromAction_WithAction_CreatesDisposable()
        {
            // Arrange
            var actionCalled = false;
            Action action = () => actionCalled = true;

            // Act
            var disposable = Disposable.FromAction(action);

            // Assert
            Assert.That(disposable, Is.Not.Null);
            Assert.That(actionCalled, Is.False);
        }

        [Test]
        public void FromAction_Dispose_CallsAction()
        {
            // Arrange
            var actionCalled = false;
            Action action = () => actionCalled = true;
            var disposable = Disposable.FromAction(action);

            // Act
            disposable.Dispose();

            // Assert
            Assert.That(actionCalled, Is.True);
        }

        [Test]
        public void FromAction_DisposeTwice_ThrowsInvalidOperationException()
        {
            // Arrange
            var actionCalledCount = 0;
            Action action = () => actionCalledCount++;
            var disposable = Disposable.FromAction(action);
            disposable.Dispose();

            // Act & Assert
            Assert.That(() => disposable.Dispose(), Throws.TypeOf<InvalidOperationException>());
            Assert.That(actionCalledCount, Is.EqualTo(1));
        }

        [Test]
        public void Merge_WithEnumerable_CreatesDisposable()
        {
            // Arrange
            var disposables = new List<IDisposable>
            {
                Disposable.FromAction(() => {}),
                Disposable.FromAction(() => {}),
                Disposable.FromAction(() => {})
            };

            // Act
            var merged = Disposable.Merge(disposables);

            // Assert
            Assert.That(merged, Is.Not.Null);
        }

        [Test]
        public void Merge_WithEnumerable_DisposeCallsAllInReverseOrder()
        {
            // Arrange
            var disposeOrder = new List<int>();
            var disposables = new List<IDisposable>
            {
                Disposable.FromAction(() => disposeOrder.Add(1)),
                Disposable.FromAction(() => disposeOrder.Add(2)),
                Disposable.FromAction(() => disposeOrder.Add(3))
            };

            var merged = Disposable.Merge(disposables);

            // Act
            merged.Dispose();

            // Assert
            Assert.That(disposeOrder.ToArray(), Is.EqualTo(new[] { 3, 2, 1 }));
        }

        [Test]
        public void Merge_WithParams_CreatesDisposable()
        {
            // Arrange
            var disposable1 = Disposable.FromAction(() => { });
            var disposable2 = Disposable.FromAction(() => { });
            var disposable3 = Disposable.FromAction(() => { });

            // Act
            var merged = Disposable.Merge(disposable1, disposable2, disposable3);

            // Assert
            Assert.That(merged, Is.Not.Null);
        }

        [Test]
        public void Scope_WithAction_CreatesDisposable()
        {
            // Arrange
            var scopeCalled = false;
            Action<Disposable.SubmitDisposable> scope = (submit) =>
            {
                scopeCalled = true;
                submit(Disposable.FromAction(() => { }));
            };

            // Act
            var disposable = Disposable.Scope(scope);

            // Assert
            Assert.That(disposable, Is.Not.Null);
            Assert.That(scopeCalled, Is.True);
        }

        [Test]
        public void Scope_Dispose_CallsSubmittedDisposablesInReverseOrder()
        {
            // Arrange
            var disposeOrder = new List<int>();
            Action<Disposable.SubmitDisposable> scope = (submit) =>
            {
                submit(Disposable.FromAction(() => disposeOrder.Add(1)));
                submit(Disposable.FromAction(() => disposeOrder.Add(2)));
                submit(Disposable.FromAction(() => disposeOrder.Add(3)));
            };

            var disposable = Disposable.Scope(scope);

            // Act
            disposable.Dispose();

            // Assert
            Assert.That(disposeOrder, Is.EqualTo(new[] { 3, 2, 1 }));
        }

        [Test]
        public void Scope_WithNoSubmission_DisposeDoesNothing()
        {
            // Arrange
            Action<Disposable.SubmitDisposable> scope = (submit) =>
            {
                // Don't submit anything
            };

            var disposable = Disposable.Scope(scope);

            // Act & Assert
            Assert.DoesNotThrow(() => disposable.Dispose());
        }

        [Test]
        public void Merge_WithEmptyEnumerable_ReturnsValidDisposable()
        {
            // Arrange
            var emptyList = new List<IDisposable>();

            // Act
            var merged = Disposable.Merge(emptyList);

            // Assert
            Assert.That(merged, Is.Not.Null);
            Assert.DoesNotThrow(() => merged.Dispose());
        }

        [Test]
        public void Scope_Generic_ReturnsResultAndDisposable()
        {
            // Arrange
            var expectedResult = 42;
            var disposeOrder = new List<int>();
            Func<Disposable.SubmitDisposable, int> scope = (submit) =>
            {
                submit(Disposable.FromAction(() => disposeOrder.Add(1)));
                submit(Disposable.FromAction(() => disposeOrder.Add(2)));
                return expectedResult;
            };

            // Act
            var (result, disposable) = Disposable.Scope(scope);

            // Assert
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(disposable, Is.Not.Null);
            Assert.That(disposeOrder, Is.Empty); // Not disposed yet
        }

        [Test]
        public void Scope_Generic_DisposeCallsSubmittedDisposablesInReverseOrder()
        {
            // Arrange
            var disposeOrder = new List<int>();
            var expectedResult = "test result";
            Func<Disposable.SubmitDisposable, string> scope = (submit) =>
            {
                submit(Disposable.FromAction(() => disposeOrder.Add(1)));
                submit(Disposable.FromAction(() => disposeOrder.Add(2)));
                submit(Disposable.FromAction(() => disposeOrder.Add(3)));
                return expectedResult;
            };

            var (result, disposable) = Disposable.Scope(scope);

            // Act
            disposable.Dispose();

            // Assert
            Assert.That(result, Is.EqualTo(expectedResult));
            Assert.That(disposeOrder, Is.EqualTo(new[] { 3, 2, 1 }));
        }

        [Test]
        public void Scope_Generic_SubmitDisposableOutsideScope_ThrowsInvalidOperationException()
        {
            // Arrange
            Disposable.SubmitDisposable? capturedSubmit = null;
            Func<Disposable.SubmitDisposable, int> scope = (submit) =>
            {
                capturedSubmit = submit;
                return 0;
            };

            var (_, disposable) = Disposable.Scope(scope);

            // Act & Assert - Using capturedSubmit outside scope should throw
            Assert.That(() => capturedSubmit!(Disposable.FromAction(() => { })),
                Throws.TypeOf<InvalidOperationException>());

            // Cleanup
            disposable.Dispose();
        }

        [Test]
        public void Scope_Generic_WithNullDisposable_Behavior()
        {
            // Arrange
            Func<Disposable.SubmitDisposable, int> scope = (submit) =>
            {
                // Try to submit null
                submit(null!);
                return 0;
            };

            // Act & Assert
            // Test the actual behavior when null is submitted
            // Stack.Push(null) may or may not throw ArgumentNullException
            // depending on .NET version and configuration
            try
            {
                var (result, disposable) = Disposable.Scope(scope);
                // If we get here, null was accepted
                // Now try to dispose - this should fail when trying to call Dispose on null
                Assert.That(() => disposable.Dispose(), Throws.TypeOf<NullReferenceException>());
            }
            catch (ArgumentNullException)
            {
                // Stack.Push(null) threw ArgumentNullException - this is expected behavior
                Assert.Pass();
            }
        }

        [Test]
        public void Scope_Generic_WithExceptionInScope_PropagatesException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Scope failed");
            Func<Disposable.SubmitDisposable, int> scope = (submit) =>
            {
                submit(Disposable.FromAction(() => { }));
                throw expectedException;
            };

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => Disposable.Scope(scope));
            Assert.That(exception, Is.SameAs(expectedException));
        }

        [Test]
        public void Scope_Generic_DisposeCalledTwice_ThrowsInvalidOperationException()
        {
            // Arrange
            var disposeCount = 0;
            Func<Disposable.SubmitDisposable, UnitType> scope = (submit) =>
            {
                submit(Disposable.FromAction(() => disposeCount++));
                return new UnitType();
            };

            var (_, disposable) = Disposable.Scope(scope);
            disposable.Dispose();

            // Act & Assert
            Assert.That(() => disposable.Dispose(), Throws.TypeOf<InvalidOperationException>());
            Assert.That(disposeCount, Is.EqualTo(1)); // Should only be disposed once
        }

        [Test]
        public void Scope_Generic_ReturnsComplexType()
        {
            // Arrange
            var expectedList = new List<string> { "a", "b", "c" };
            Func<Disposable.SubmitDisposable, List<string>> scope = (submit) =>
            {
                submit(Disposable.FromAction(() => { }));
                return expectedList;
            };

            // Act
            var (result, disposable) = Disposable.Scope(scope);

            // Assert
            Assert.That(result, Is.SameAs(expectedList));
            Assert.That(result, Is.EqualTo(new[] { "a", "b", "c" }));
            disposable.Dispose();
        }

        [Test]
        public void Scope_NonGeneric_IsWrapperOfGenericScope()
        {
            // Arrange
            var disposeOrder = new List<int>();
            Action<Disposable.SubmitDisposable> scope = (submit) =>
            {
                submit(Disposable.FromAction(() => disposeOrder.Add(1)));
                submit(Disposable.FromAction(() => disposeOrder.Add(2)));
            };

            // Act
            var disposable = Disposable.Scope(scope);
            disposable.Dispose();

            // Assert
            Assert.That(disposeOrder, Is.EqualTo(new[] { 2, 1 }));
        }

    }
}