using NUnit.Framework;
using JiksLib.Control;
using System;
using System.Collections.Generic;

namespace JiksLib.Test.Control
{
    [TestFixture]
    public class ServiceCollectionTests
    {
        private sealed class TestCollection : ServiceCollection
        {
            public TestCollection(ServiceCollection? fallback = null)
                : base(fallback)
            {
            }

            public void RegisterService(object service, bool autoDispose = true) =>
                Register(service, autoDispose);

            public void RegisterService<T>(T service, bool autoDispose = true) =>
                Register(service, autoDispose);

            public void Attach(IDisposable disposable) =>
                AttachDisposable(disposable);

            public void OnDispose(Action action) =>
                RunOnDispose(action);
        }

        private interface ITestService
        {
            string Name { get; }
        }

        private sealed class TestService : ITestService
        {
            public TestService(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        private sealed class TrackingDisposable : IDisposable
        {
            private readonly List<string> log;
            private readonly string name;

            public TrackingDisposable(List<string> log, string name)
            {
                this.log = log;
                this.name = name;
            }

            public void Dispose() => log.Add(name);
        }

        private sealed class ThrowingDisposable : IDisposable
        {
            public void Dispose() => throw new InvalidOperationException("boom");
        }

        [Test]
        public void Get_WhenServiceNotRegistered_ThrowsInvalidOperationException()
        {
            using var collection = new TestCollection();

            Assert.That(
                () => collection.Get<string>(),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void TryGet_WhenServiceNotRegistered_ReturnsFalse()
        {
            using var collection = new TestCollection();

            Assert.That(collection.TryGet<string>(out var service), Is.False);
            Assert.That(service, Is.Null);
        }

        [Test]
        public void Register_ObjectThenGet_ReturnsSameInstance()
        {
            using var collection = new TestCollection();
            var service = new List<int>();

            collection.RegisterService(service);

            Assert.That(collection.Get<List<int>>(), Is.SameAs(service));
        }

        [Test]
        public void Register_InterfaceThenGetByInterface_ReturnsInstance()
        {
            using var collection = new TestCollection();
            var service = new TestService("a");

            collection.RegisterService<ITestService>(service);

            Assert.That(collection.Get<ITestService>(), Is.SameAs(service));
        }

        [Test]
        public void Child_FallsBackToParent()
        {
            using var parent = new TestCollection();
            var service = new TestService("parent");
            parent.RegisterService<ITestService>(service);

            using var child = new TestCollection(parent);

            Assert.That(child.Get<ITestService>(), Is.SameAs(service));
        }

        [Test]
        public void Child_OverridesParent()
        {
            using var parent = new TestCollection();
            parent.RegisterService<ITestService>(new TestService("parent"));

            using var child = new TestCollection(parent);
            var own = new TestService("child");
            child.RegisterService<ITestService>(own);

            Assert.That(child.Get<ITestService>(), Is.SameAs(own));
        }

        [Test]
        public void Register_NullService_ThrowsArgumentNullException()
        {
            using var collection = new TestCollection();

            Assert.That(
                () => collection.RegisterService(null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Register_DuplicateKey_LastRegistrationWins()
        {
            using var collection = new TestCollection();
            var first = new TestService("first");
            var second = new TestService("second");

            collection.RegisterService<ITestService>(first);
            collection.RegisterService<ITestService>(second);

            Assert.That(collection.Get<ITestService>(), Is.SameAs(second));
        }

        [Test]
        public void Dispose_DisposesOwnedServicesInReverseRegistrationOrder()
        {
            var log = new List<string>();
            var collection = new TestCollection();

            collection.RegisterService(new TrackingDisposable(log, "first"));
            collection.RegisterService(new TrackingDisposable(log, "second"));

            collection.Dispose();

            Assert.That(log, Is.EqualTo(new[] { "second", "first" }));
        }

        [Test]
        public void Dispose_WithAutoDisposeFalse_DoesNotDisposeService()
        {
            var log = new List<string>();
            var collection = new TestCollection();

            collection.RegisterService(
                new TrackingDisposable(log, "external"),
                autoDispose: false);

            collection.Dispose();

            Assert.That(log, Is.Empty);
        }

        [Test]
        public void Dispose_AttachedDisposable_IsDisposed()
        {
            var log = new List<string>();
            var collection = new TestCollection();

            collection.Attach(new TrackingDisposable(log, "attached"));
            collection.Dispose();

            Assert.That(log, Is.EqualTo(new[] { "attached" }));
        }

        [Test]
        public void Dispose_RunsActionsInReverseOrder()
        {
            var log = new List<string>();
            var collection = new TestCollection();

            collection.OnDispose(() => log.Add("action1"));
            collection.Attach(new TrackingDisposable(log, "disposable"));
            collection.OnDispose(() => log.Add("action2"));

            collection.Dispose();

            Assert.That(log, Is.EqualTo(new[] { "action2", "disposable", "action1" }));
        }

        [Test]
        public void Dispose_DoesNotDisposeFallback()
        {
            var log = new List<string>();
            var parent = new TestCollection();
            parent.RegisterService(new TrackingDisposable(log, "parent"));

            using (var child = new TestCollection(parent))
                child.Dispose();

            Assert.That(log, Is.Empty);

            parent.Dispose();
            Assert.That(log, Is.EqualTo(new[] { "parent" }));
        }

        [Test]
        public void Dispose_SameInstanceRegisteredTwice_DisposesTwice()
        {
            var log = new List<string>();
            var disposable = new TrackingDisposable(log, "shared");
            var collection = new TestCollection();

            collection.RegisterService(disposable);
            collection.RegisterService<IDisposable>(disposable);

            collection.Dispose();

            // 不做去重：每次注册都会入栈一次，因此同一实例会被释放两次。
            Assert.That(log, Is.EqualTo(new[] { "shared", "shared" }));
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var collection = new TestCollection();
            collection.RegisterService(new List<int>());

            collection.Dispose();

            Assert.DoesNotThrow(() => collection.Dispose());
        }

        [Test]
        public void Register_AfterDispose_CollectionRemainsUsable()
        {
            var collection = new TestCollection();
            collection.Dispose();

            var service = new List<int>();
            collection.RegisterService(service);

            Assert.That(collection.Get<List<int>>(), Is.SameAs(service));
        }

        [Test]
        public void Attach_AfterDispose_IsReleasedByNextDispose()
        {
            var log = new List<string>();
            var collection = new TestCollection();
            collection.Dispose();

            collection.Attach(new TrackingDisposable(log, "reused"));
            Assert.That(log, Is.Empty);

            collection.Dispose();
            Assert.That(log, Is.EqualTo(new[] { "reused" }));
        }

        [Test]
        public void Get_AfterDispose_ThrowsInvalidOperationException()
        {
            var collection = new TestCollection();
            collection.Dispose();

            Assert.That(
                () => collection.Get<List<int>>(),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void TryGet_AfterDispose_ReturnsFalse()
        {
            var collection = new TestCollection();
            collection.Dispose();

            Assert.That(collection.TryGet<List<int>>(out var service), Is.False);
            Assert.That(service, Is.Null);
        }

        [Test]
        public void Dispose_SingleThrowingService_PropagatesException()
        {
            var collection = new TestCollection();
            collection.RegisterService(new ThrowingDisposable());

            Assert.That(
                () => collection.Dispose(),
                Throws.TypeOf<InvalidOperationException>());
        }

        [Test]
        public void Dispose_MultipleThrowingServices_AggregatesExceptions()
        {
            var collection = new TestCollection();
            collection.RegisterService(new ThrowingDisposable());
            collection.RegisterService(new ThrowingDisposable());

            var exception = Assert.Throws<AggregateException>(
                () => collection.Dispose());

            Assert.That(exception!.InnerExceptions.Count, Is.EqualTo(2));
        }

        [Test]
        public void Dispose_ContinuesAfterOneServiceThrows()
        {
            var log = new List<string>();
            var collection = new TestCollection();

            collection.RegisterService(new TrackingDisposable(log, "first"));
            collection.RegisterService(new ThrowingDisposable());
            collection.RegisterService(new TrackingDisposable(log, "last"));

            Assert.Throws<InvalidOperationException>(() => collection.Dispose());

            // 逆序释放：last → throwing → first；抛异常不阻止其余释放
            Assert.That(log, Is.EqualTo(new[] { "last", "first" }));
        }
    }
}
