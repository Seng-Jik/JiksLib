using NUnit.Framework;
using JiksLib.Control;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;

namespace JiksLib.Test.Control
{
    // Helper test scope implementation
    public class TestScope : ServiceLocator.Scope
    {
        private readonly Action<TestScope> _registerAction;

        public TestScope(Action<TestScope> registerAction)
        {
            _registerAction = registerAction;
        }

        protected override void OnRegisterServices()
        {
            _registerAction(this);
        }

        public void RegisterService<T>(T service) where T : class => Register(service);
    }

    [TestFixture]
    public class ServiceLocatorTests
    {

        [SetUp]
        public void SetUp()
        {
            // Clear any leftover services between tests
            // Note: ServiceLocator doesn't have a Clear method, so we rely on proper scoping
            // Tests should clean up their own scopes

            // Additional cleanup: Reset services that might have been left by previous tests
            // This is needed because tests might not clean up properly or tests run in parallel
            ResetServiceHolder<string>();
            ResetServiceHolder<object>();
            ResetServiceHolder<List<int>>();
            ResetServiceHolder<Dictionary<string, int>>();

            // Also clear the scopes stack in case a test didn't clean up properly
            ResetScopesStack();
        }

        private static void ResetServiceHolder<T>() where T : class
        {
            try
            {
                var serviceHolderType = typeof(ServiceLocator).GetNestedType("ServiceHolder`1", BindingFlags.NonPublic);
                if (serviceHolderType != null)
                {
                    var genericType = serviceHolderType.MakeGenericType(typeof(T));
                    var serviceField = genericType.GetField("Service", BindingFlags.Public | BindingFlags.Static);
                    if (serviceField != null)
                    {
                        serviceField.SetValue(null, null);
                    }
                }
            }
            catch
            {
                // Ignore reflection errors in tests
            }
        }

        private static void ResetScopesStack()
        {
            try
            {
                var scopesField = typeof(ServiceLocator).GetField("scopes", BindingFlags.NonPublic | BindingFlags.Static);
                if (scopesField != null && scopesField.GetValue(null) is System.Collections.ICollection stack)
                {
                    // Clear the stack if it has a Clear method
                    var clearMethod = stack.GetType().GetMethod("Clear");
                    if (clearMethod != null)
                    {
                        clearMethod.Invoke(stack, null);
                    }
                }
            }
            catch
            {
                // Ignore reflection errors
            }
        }

        [Test]
        public void Get_WhenServiceNotRegistered_ThrowsInvalidOperationException()
        {
            // Arrange & Act & Assert
            Assert.That(() => ServiceLocator.Get<string>(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("Service not registered"));
        }

        [Test]
        public void EnterScope_RegistersService_CanGetService()
        {
            // Arrange
            var expectedService = "Test Service";
            var scope = new TestScope(s => s.RegisterService(expectedService));

            // Act
            using (ServiceLocator.EnterScope(scope))
            {
                var service = ServiceLocator.Get<string>();

                // Assert
                Assert.That(service, Is.EqualTo(expectedService));
            }
        }

        [Test]
        public void ExitScope_RestoresPreviousService()
        {
            // Arrange
            var outerService = "Outer Service";
            var innerService = "Inner Service";
            var outerScope = new TestScope(s => s.RegisterService(outerService));
            var innerScope = new TestScope(s => s.RegisterService(innerService));

            using (ServiceLocator.EnterScope(outerScope))
            {
                // Act
                using (ServiceLocator.EnterScope(innerScope))
                {
                    var inner = ServiceLocator.Get<string>();
                    Assert.That(inner, Is.EqualTo(innerService));
                }

                // Assert
                var outer = ServiceLocator.Get<string>();
                Assert.That(outer, Is.EqualTo(outerService));
            }
        }

        [Test]
        public void NestedScopes_InnerOverridesOuter()
        {
            // Arrange
            var outerService = "Outer";
            var innerService = "Inner";
            var outerScope = new TestScope(s => s.RegisterService(outerService));
            var innerScope = new TestScope(s => s.RegisterService(innerService));

            using (ServiceLocator.EnterScope(outerScope))
            {
                // Act
                using (ServiceLocator.EnterScope(innerScope))
                {
                    // Assert
                    var service = ServiceLocator.Get<string>();
                    Assert.That(service, Is.EqualTo(innerService));
                }
            }
        }

        [Test]
        public void DisposeOutOfOrder_ThrowsInvalidOperationException()
        {
            // Arrange
            var scope1 = new TestScope(s => s.RegisterService("1"));
            var scope2 = new TestScope(s => s.RegisterService("2"));

            var disposable1 = ServiceLocator.EnterScope(scope1);
            var disposable2 = ServiceLocator.EnterScope(scope2);

            // Act & Assert - Dispose 2 then 1 (correct order) should not throw
            Assert.DoesNotThrow(() => disposable2.Dispose());
            Assert.DoesNotThrow(() => disposable1.Dispose());

            // Recreate for negative test
            disposable1 = ServiceLocator.EnterScope(scope1);
            disposable2 = ServiceLocator.EnterScope(scope2);

            // Act & Assert - Dispose 1 before 2 (wrong order) should throw
            var ex = Assert.Throws<InvalidOperationException>(() => disposable1.Dispose());
            Assert.That(ex.Message, Contains.Substring("must in order"));

            // Cleanup
            disposable2.Dispose(); // Dispose remaining scope
        }


        [Test]
        public void MultipleServices_CanRegisterAndRetrieve()
        {
            // Arrange
            var stringService = "String Service";
            var listService = new List<int> { 1, 2, 3 };
            var scope = new TestScope(s =>
            {
                s.RegisterService(stringService);
                s.RegisterService(listService);
            });

            // Act
            using (ServiceLocator.EnterScope(scope))
            {
                // Assert
                Assert.That(ServiceLocator.Get<string>(), Is.EqualTo(stringService));
                Assert.That(ServiceLocator.Get<List<int>>(), Is.SameAs(listService));
            }
        }

        [Test]
        public void Scope_CanRegisterMultipleInstancesOfSameType()
        {
            // Arrange - This should work because each registration overwrites the previous
            var service1 = "First";
            var service2 = "Second";
            var scope = new TestScope(s =>
            {
                s.RegisterService(service1);
                s.RegisterService(service2); // Overwrites service1
            });

            // Act
            using (ServiceLocator.EnterScope(scope))
            {
                // Assert
                Assert.That(ServiceLocator.Get<string>(), Is.EqualTo(service2));
            }
        }

        [Test]
        public void EnterScope_ExceptionDuringOnRegisterServices_RollsBackRegistrations()
        {
            // Arrange
            var serviceBeforeException = "Should be rolled back";
            var scope = new TestScope(s =>
            {
                s.RegisterService(serviceBeforeException);
                throw new InvalidOperationException("Test exception");
            });

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => ServiceLocator.EnterScope(scope));

            // Service should not be registered after exception
            Assert.That(() => ServiceLocator.Get<string>(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("Service not registered"));
        }



        [Test]
        public void ConcurrentAccess_ThreadSafe()
        {
            // Arrange
            const int threadCount = 10;
            const int iterations = 1000;
            var exceptions = new List<Exception>();
            var scope = new TestScope(s => s.RegisterService(threadCount.ToString()));

            using (ServiceLocator.EnterScope(scope))
            {
                // Act
                Parallel.For(0, threadCount, i =>
                {
                    for (int j = 0; j < iterations; j++)
                    {
                        try
                        {
                            var service = ServiceLocator.Get<string>();
                            if (service != threadCount.ToString())
                            {
                                exceptions.Add(new InvalidOperationException($"Expected {threadCount}, got {service}"));
                            }
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });

                // Assert
                Assert.That(exceptions, Is.Empty, $"Had {exceptions.Count} exceptions during concurrent access");
            }
        }


        [Test]
        public void Get_ReturnsSameInstanceRegistered()
        {
            // Arrange
            var service = new object();
            var scope = new TestScope(s => s.RegisterService(service));

            // Act
            using (ServiceLocator.EnterScope(scope))
            {
                var retrieved = ServiceLocator.Get<object>();

                // Assert
                Assert.That(retrieved, Is.SameAs(service));
            }
        }


        [Test]
        public void NestedScopes_ThreeLevels_ProperlyRestores()
        {
            // Arrange
            var level1 = "Level1";
            var level2 = "Level2";
            var level3 = "Level3";

            var scope1 = new TestScope(s => s.RegisterService(level1));
            var scope2 = new TestScope(s => s.RegisterService(level2));
            var scope3 = new TestScope(s => s.RegisterService(level3));

            // Act & Assert
            using (var d1 = ServiceLocator.EnterScope(scope1))
            {
                Assert.That(ServiceLocator.Get<string>(), Is.EqualTo(level1));

                using (var d2 = ServiceLocator.EnterScope(scope2))
                {
                    Assert.That(ServiceLocator.Get<string>(), Is.EqualTo(level2));

                    using (var d3 = ServiceLocator.EnterScope(scope3))
                    {
                        Assert.That(ServiceLocator.Get<string>(), Is.EqualTo(level3));
                    }

                    Assert.That(ServiceLocator.Get<string>(), Is.EqualTo(level2));
                }

                Assert.That(ServiceLocator.Get<string>(), Is.EqualTo(level1));
            }
        }

        [Test]
        public void EmptyScope_NoServicesRegistered()
        {
            // Arrange
            var scope = new TestScope(s => { /* No registrations */ });

            // Act & Assert
            using (ServiceLocator.EnterScope(scope))
            {
                Assert.That(() => ServiceLocator.Get<string>(),
                    Throws.TypeOf<InvalidOperationException>()
                        .With.Message.Contains("Service not registered"));
            }
        }

        [Test]
        public void Scope_CanBeDisposedMultipleTimes_WithoutThrowing()
        {
            // Arrange
            var service = "Test Service";
            var scope = new TestScope(s => s.RegisterService(service));
            var disposable = ServiceLocator.EnterScope(scope);

            // Act & Assert - First dispose should work
            Assert.DoesNotThrow(() => disposable.Dispose());

            // Service should no longer be available
            Assert.That(() => ServiceLocator.Get<string>(),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("Service not registered"));

            // Second dispose should not throw
            Assert.DoesNotThrow(() => disposable.Dispose());
        }

        [Test]
        public void Register_OnlyAllowedDuringOnRegisterServices()
        {
            // Positive test: Register works inside OnRegisterServices
            var service = "Test";
            var scope = new TestScope(s => s.RegisterService(service));

            using (ServiceLocator.EnterScope(scope))
            {
                var retrieved = ServiceLocator.Get<string>();
                Assert.That(retrieved, Is.EqualTo(service));
            }

            // Negative test: Register should throw when called outside of OnRegisterServices
            var testScope = new TestScope(s => { });

            // Get the generic Register<T> method via reflection
            var registerMethodGeneric = typeof(ServiceLocator.Scope).GetMethod(
                "Register",
                BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new Type[] { typeof(string) },
                null);

            // If the above fails (because it's generic), try getting the generic method definition
            if (registerMethodGeneric == null)
            {
                var methods = typeof(ServiceLocator.Scope).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var method in methods)
                {
                    if (method.Name == "Register" && method.IsGenericMethod)
                    {
                        registerMethodGeneric = method.MakeGenericMethod(typeof(string));
                        break;
                    }
                }
            }

            Assert.That(registerMethodGeneric, Is.Not.Null, "Register method should be found via reflection");

            // Try to call Register when UnregisterActions is null (scope not entered)
            // This should throw InvalidOperationException
            Assert.That(() => registerMethodGeneric.Invoke(testScope, new object[] { "test" }),
                Throws.TargetInvocationException.With.InnerException
                    .TypeOf<InvalidOperationException>());
        }

        [Test]
        public void MixedReferenceTypes_CanRegisterAndRetrieve()
        {
            // Arrange
            var stringService = "Hello";
            var listService = new List<int> { 1, 2, 3 };
            var dictService = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
            var objService = new object();

            var scope = new TestScope(s =>
            {
                s.RegisterService(stringService);
                s.RegisterService(listService);
                s.RegisterService(dictService);
                s.RegisterService(objService);
            });

            // Act & Assert
            using (ServiceLocator.EnterScope(scope))
            {
                Assert.That(ServiceLocator.Get<string>(), Is.EqualTo(stringService));
                Assert.That(ServiceLocator.Get<List<int>>(), Is.SameAs(listService));
                Assert.That(ServiceLocator.Get<Dictionary<string, int>>(), Is.SameAs(dictService));
                Assert.That(ServiceLocator.Get<object>(), Is.SameAs(objService));
            }
        }
    }
}