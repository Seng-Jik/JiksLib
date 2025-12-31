using NUnit.Framework;
using JiksLib.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace JiksLib.Test.Reflection
{
    // 测试用的 ScannableAttribute 派生类
    public class TestScannableAttribute1 : ScannableAttribute
    {
        public string Value { get; }

        public TestScannableAttribute1(string value)
        {
            Value = value;
        }
    }

    public class TestScannableAttribute2 : ScannableAttribute
    {
        public int Number { get; }

        public TestScannableAttribute2(int number)
        {
            Number = number;
        }
    }

    // 测试用的标记类
    [TestScannableAttribute1("ClassA")]
    public class TestClassA { }

    [TestScannableAttribute1("ClassB")]
    [TestScannableAttribute2(42)]
    public class TestClassB { }

    [TestScannableAttribute2(100)]
    public class TestClassC { }

    // 未标记任何 ScannableAttribute 的类
    public class UnmarkedClass { }

    // 用于测试不存在的 Attribute 类型
    public class NonExistentAttribute : ScannableAttribute { }

    // 抽象类测试
    [TestScannableAttribute1("Abstract")]
    public abstract class AbstractTestClass { }

    // 接口测试
    [TestScannableAttribute1("Interface")]
    public interface ITestInterface { }

    [TestFixture]
    public class ScannableAttributeTests
    {
        private Assembly _testAssembly;

        [SetUp]
        public void SetUp()
        {
            // 获取当前测试程序集（包含测试类）
            _testAssembly = Assembly.GetExecutingAssembly();
        }

        [Test]
        public void ScanAssemblies_WithSpecificAssembly_ReturnsMarkedTypes()
        {
            // Arrange
            var assemblies = new[] { _testAssembly };

            // Act
            var result = ScannableAttribute.ScanAssemblies(assemblies);

            // Assert
            var typesWithAttr1 = result.GetTypesByAttribute<TestScannableAttribute1>().ToList();
            var typesWithAttr2 = result.GetTypesByAttribute<TestScannableAttribute2>().ToList();

            // 应该找到标记了 TestScannableAttribute1 的类
            Assert.That(typesWithAttr1.Count, Is.EqualTo(4));
            Assert.That(typesWithAttr1.Any(t => t.Type == typeof(TestClassA)), Is.True);
            Assert.That(typesWithAttr1.Any(t => t.Type == typeof(TestClassB)), Is.True);

            // 应该找到标记了 TestScannableAttribute2 的类
            Assert.That(typesWithAttr2.Count, Is.EqualTo(2));
            Assert.That(typesWithAttr2.Any(t => t.Type == typeof(TestClassB)), Is.True);
            Assert.That(typesWithAttr2.Any(t => t.Type == typeof(TestClassC)), Is.True);

            // 验证 Attribute 实例的属性值
            var classAAttr = typesWithAttr1.First(t => t.Type == typeof(TestClassA)).Attr;
            Assert.That(classAAttr.Value, Is.EqualTo("ClassA"));

            var classBAttr1 = typesWithAttr1.First(t => t.Type == typeof(TestClassB)).Attr;
            Assert.That(classBAttr1.Value, Is.EqualTo("ClassB"));

            var classBAttr2 = typesWithAttr2.First(t => t.Type == typeof(TestClassB)).Attr;
            Assert.That(classBAttr2.Number, Is.EqualTo(42));

            var classCAttr = typesWithAttr2.First(t => t.Type == typeof(TestClassC)).Attr;
            Assert.That(classCAttr.Number, Is.EqualTo(100));
        }

        [Test]
        public void ScanAssemblies_WithNullAssemblies_ScansAllAssemblies()
        {
            // Arrange & Act
            var result = ScannableAttribute.ScanAssemblies(null);

            // Assert
            // 至少应该包含测试程序集中的标记类型
            var typesWithAttr1 = result.GetTypesByAttribute<TestScannableAttribute1>().ToList();
            var typesWithAttr2 = result.GetTypesByAttribute<TestScannableAttribute2>().ToList();

            // 确保找到测试类（可能还有其他程序集中的类）
            Assert.That(typesWithAttr1.Any(t => t.Type == typeof(TestClassA)), Is.True);
            Assert.That(typesWithAttr1.Any(t => t.Type == typeof(TestClassB)), Is.True);
            Assert.That(typesWithAttr2.Any(t => t.Type == typeof(TestClassB)), Is.True);
            Assert.That(typesWithAttr2.Any(t => t.Type == typeof(TestClassC)), Is.True);
        }

        [Test]
        public void ScanAssemblies_WithEmptyAssemblyList_ReturnsEmptyResult()
        {
            // Arrange
            var emptyAssemblies = Enumerable.Empty<Assembly>();

            // Act
            var result = ScannableAttribute.ScanAssemblies(emptyAssemblies);

            // Assert
            var typesWithAttr1 = result.GetTypesByAttribute<TestScannableAttribute1>().ToList();
            var typesWithAttr2 = result.GetTypesByAttribute<TestScannableAttribute2>().ToList();

            Assert.That(typesWithAttr1.Count, Is.EqualTo(0));
            Assert.That(typesWithAttr2.Count, Is.EqualTo(0));
        }

        [Test]
        public void ScanAssemblies_WithAssemblyContainingNoMarkedTypes_ReturnsEmptyResult()
        {
            // Arrange
            // 使用一个不包含 ScannableAttribute 标记类型的程序集
            // System.Runtime 程序集应该不包含我们的测试 Attribute
            var assembly = typeof(object).Assembly; // mscorlib 或 System.Runtime

            // Act
            var result = ScannableAttribute.ScanAssemblies(new[] { assembly });

            // Assert
            var typesWithAttr1 = result.GetTypesByAttribute<TestScannableAttribute1>().ToList();
            var typesWithAttr2 = result.GetTypesByAttribute<TestScannableAttribute2>().ToList();

            // 可能为 0，或者如果其他代码使用了 ScannableAttribute，则可能不为 0
            // 但我们只关心我们的测试 Attribute 是否被找到
            Assert.That(typesWithAttr1.Any(t => t.Type == typeof(TestClassA)), Is.False);
            Assert.That(typesWithAttr2.Any(t => t.Type == typeof(TestClassB)), Is.False);
        }

        [Test]
        public void GetTypesByAttribute_WithNonExistentAttributeType_ReturnsEmptyEnumerable()
        {
            // Arrange
            var assemblies = new[] { _testAssembly };
            var result = ScannableAttribute.ScanAssemblies(assemblies);

            // 使用外部定义的 NonExistentAttribute（未被任何类型标记）

            // Act
            var types = result.GetTypesByAttribute<NonExistentAttribute>().ToList();

            // Assert
            Assert.That(types.Count, Is.EqualTo(0));
        }

        [Test]
        public void ScanResult_IsImmutable()
        {
            // Arrange
            var assemblies = new[] { _testAssembly };
            var result = ScannableAttribute.ScanAssemblies(assemblies);

            // Act
            var types1 = result.GetTypesByAttribute<TestScannableAttribute1>().ToList();
            var types2 = result.GetTypesByAttribute<TestScannableAttribute1>().ToList();

            // Assert
            // 多次调用应该返回相同结果
            Assert.That(types1.Count, Is.EqualTo(types2.Count));
            for (int i = 0; i < types1.Count; i++)
            {
                Assert.That(types1[i].Type, Is.EqualTo(types2[i].Type));
                Assert.That(types1[i].Attr.Value, Is.EqualTo(types2[i].Attr.Value));
            }
        }

        [Test]
        public void ScanAssemblies_WithMultipleAssemblies_CombinesResults()
        {
            // Arrange
            var assembly1 = _testAssembly;
            var assembly2 = typeof(object).Assembly; // 另一个程序集

            var assemblies = new[] { assembly1, assembly2 };

            // Act
            var result = ScannableAttribute.ScanAssemblies(assemblies);

            // Assert
            var typesWithAttr1 = result.GetTypesByAttribute<TestScannableAttribute1>().ToList();

            // 至少包含测试程序集中的类型
            Assert.That(typesWithAttr1.Any(t => t.Type == typeof(TestClassA)), Is.True);
            Assert.That(typesWithAttr1.Any(t => t.Type == typeof(TestClassB)), Is.True);
        }

        [Test]
        public void ScanAssemblies_HandlesTypesWithMultipleAttributes()
        {
            // Arrange
            var assemblies = new[] { _testAssembly };

            // Act
            var result = ScannableAttribute.ScanAssemblies(assemblies);

            // Assert
            var typesWithAttr1 = result.GetTypesByAttribute<TestScannableAttribute1>().ToList();
            var typesWithAttr2 = result.GetTypesByAttribute<TestScannableAttribute2>().ToList();

            // TestClassB 应该出现在两个列表中
            Assert.That(typesWithAttr1.Any(t => t.Type == typeof(TestClassB)), Is.True);
            Assert.That(typesWithAttr2.Any(t => t.Type == typeof(TestClassB)), Is.True);

            // 每个列表应该只包含 TestClassB 一次
            Assert.That(typesWithAttr1.Count(t => t.Type == typeof(TestClassB)), Is.EqualTo(1));
            Assert.That(typesWithAttr2.Count(t => t.Type == typeof(TestClassB)), Is.EqualTo(1));
        }

        [Test]
        public void GetTypesByAttribute_ReturnsCorrectAttributeInstances()
        {
            // Arrange
            var assemblies = new[] { _testAssembly };
            var result = ScannableAttribute.ScanAssemblies(assemblies);

            // Act
            var typesWithAttr1 = result.GetTypesByAttribute<TestScannableAttribute1>().ToList();

            // Assert
            var classA = typesWithAttr1.First(t => t.Type == typeof(TestClassA));
            Assert.That(classA.Attr, Is.TypeOf<TestScannableAttribute1>());
            Assert.That(classA.Attr.Value, Is.EqualTo("ClassA"));

            var classB = typesWithAttr1.First(t => t.Type == typeof(TestClassB));
            Assert.That(classB.Attr, Is.TypeOf<TestScannableAttribute1>());
            Assert.That(classB.Attr.Value, Is.EqualTo("ClassB"));
        }

        [Test]
        public void ScanAssemblies_WithAssemblyContainingAbstractClass_IncludesAbstractClass()
        {
            // Arrange - 使用外部定义的 AbstractTestClass（已标记 TestScannableAttribute1）
            var assemblies = new[] { _testAssembly };

            // Act
            var result = ScannableAttribute.ScanAssemblies(assemblies);

            // Assert
            var typesWithAttr1 = result.GetTypesByAttribute<TestScannableAttribute1>().ToList();

            // 应该包含抽象类
            Assert.That(typesWithAttr1.Any(t => t.Type == typeof(AbstractTestClass)), Is.True);
        }

        [Test]
        public void ScanAssemblies_WithAssemblyContainingInterface_IncludesInterface()
        {
            // Arrange - 使用外部定义的 ITestInterface（已标记 TestScannableAttribute1）
            var assemblies = new[] { _testAssembly };

            // Act
            var result = ScannableAttribute.ScanAssemblies(assemblies);

            // Assert
            var typesWithAttr1 = result.GetTypesByAttribute<TestScannableAttribute1>().ToList();

            // 应该包含接口
            Assert.That(typesWithAttr1.Any(t => t.Type == typeof(ITestInterface)), Is.True);
        }
    }
}