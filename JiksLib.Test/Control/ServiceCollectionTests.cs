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

            public void AddService(object service) => Add(service);

            public void AddService<T>(T service)
                where T : class => Add(service);

            public void AddService(Type serviceType, object service) =>
                Add(serviceType, service);

            public void AddExternalService(object service) =>
                AddExternal(service);

            public void AddExternalService<T>(T service)
                where T : class => AddExternal(service);
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
        public void Add_ThenGet_ReturnsSameInstance()
        {
            using var collection = new TestCollection();
            var service = new List<int>();

            collection.AddService(service);

            Assert.That(collection.Get<List<int>>(), Is.SameAs(service));
        }

        [Test]
        public void AddInterface_GetByInterface_ReturnsInstance()
        {
            using var collection = new TestCollection();
            var service = new TestService("a");

            collection.AddService<ITestService>(service);

            Assert.That(collection.Get<ITestService>(), Is.SameAs(service));
        }

        [Test]
        public void Child_FallsBackToParent()
        {
            using var parent = new TestCollection();
            var service = new TestService("parent");
            parent.AddService<ITestService>(service);

            using var child = new TestCollection(parent);

            Assert.That(child.Get<ITestService>(), Is.SameAs(service));
        }

        [Test]
        public void Child_OverridesParent()
        {
            using var parent = new TestCollection();
            parent.AddService<ITestService>(new TestService("parent"));

            using var child = new TestCollection(parent);
            var own = new TestService("child");
            child.AddService<ITestService>(own);

            Assert.That(child.Get<ITestService>(), Is.SameAs(own));
        }

        [Test]
        public void Add_NullService_ThrowsArgumentNullException()
        {
            using var collection = new TestCollection();

            Assert.That(
                () => collection.AddService(null!),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Add_MismatchedType_ThrowsArgumentException()
        {
            using var collection = new TestCollection();

            Assert.That(
                () => collection.AddService(typeof(ITestService), new object()),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Add_DuplicateKey_LastRegistrationWins()
        {
            using var collection = new TestCollection();
            var first = new TestService("first");
            var second = new TestService("second");

            collection.AddService<ITestService>(first);
            collection.AddService<ITestService>(second);

            Assert.That(collection.Get<ITestService>(), Is.SameAs(second));
        }

        [Test]
        public void Dispose_DisposesOwnedServicesInReverseRegistrationOrder()
        {
            var log = new List<string>();
            var collection = new TestCollection();

            collection.AddService(new TrackingDisposable(log, "first"));
            collection.AddService(new TrackingDisposable(log, "second"));

            collection.Dispose();

            Assert.That(log, Is.EqualTo(new[] { "second", "first" }));
        }

        [Test]
        public void Dispose_DoesNotDisposeExternalService()
        {
            var log = new List<string>();
            var collection = new TestCollection();

            collection.AddExternalService(new TrackingDisposable(log, "external"));
            collection.Dispose();

            Assert.That(log, Is.Empty);
        }

        [Test]
        public void Dispose_DoesNotDisposeFallback()
        {
            var log = new List<string>();
            var parent = new TestCollection();
            parent.AddService(new TrackingDisposable(log, "parent"));

            using (var child = new TestCollection(parent))
                child.Dispose();

            Assert.That(log, Is.Empty);

            parent.Dispose();
            Assert.That(log, Is.EqualTo(new[] { "parent" }));
        }

        [Test]
        public void Dispose_SameInstanceRegisteredTwice_DisposesOnce()
        {
            var log = new List<string>();
            var disposable = new TrackingDisposable(log, "once");
            var collection = new TestCollection();

            collection.AddService(disposable);
            collection.AddService<IDisposable>(disposable);

            collection.Dispose();

            Assert.That(log, Is.EqualTo(new[] { "once" }));
        }

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var collection = new TestCollection();
            collection.AddService(new List<int>());

            collection.Dispose();

            Assert.DoesNotThrow(() => collection.Dispose());
        }

        [Test]
        public void Add_AfterDispose_ThrowsObjectDisposedException()
        {
            var collection = new TestCollection();
            collection.Dispose();

            Assert.That(
                () => collection.AddService(new List<int>()),
                Throws.TypeOf<ObjectDisposedException>());
        }

        [Test]
        public void Get_AfterDispose_ThrowsObjectDisposedException()
        {
            var collection = new TestCollection();
            collection.Dispose();

            Assert.That(
                () => collection.Get<List<int>>(),
                Throws.TypeOf<ObjectDisposedException>());
        }

        [Test]
        public void TryGet_AfterDispose_ThrowsObjectDisposedException()
        {
            var collection = new TestCollection();
            collection.Dispose();

            Assert.That(
                () => collection.TryGet<List<int>>(out _),
                Throws.TypeOf<ObjectDisposedException>());
        }

        [Test]
        public void Dispose_AggregatesDisposeExceptions()
        {
            var collection = new TestCollection();
            collection.AddService(new ThrowingDisposable());
            collection.AddService(new ThrowingDisposable());

            var exception = Assert.Throws<AggregateException>(
                () => collection.Dispose());

            Assert.That(exception!.InnerExceptions.Count, Is.EqualTo(2));
        }

        [Test]
        public void Dispose_ContinuesAfterOneServiceThrows()
        {
            var log = new List<string>();
            var collection = new TestCollection();

            collection.AddService(new TrackingDisposable(log, "first"));
            collection.AddService(new ThrowingDisposable());
            collection.AddService(new TrackingDisposable(log, "last"));

            Assert.Throws<AggregateException>(() => collection.Dispose());

            // 逆序释放：last → throwing → first；抛异常不阻止其余释放
            Assert.That(log, Is.EqualTo(new[] { "last", "first" }));
        }
    }
}
