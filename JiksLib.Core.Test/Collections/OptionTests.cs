using NUnit.Framework;
using JiksLib.Collections;
using System;
using System.Collections.Generic;

namespace JiksLib.Test.Collections
{
    [TestFixture]
    public class OptionTests
    {
        [Test]
        public void Constructor_WithNonNullValue_CreatesSome()
        {
            // Arrange
            var value = "test";

            // Act
            var option = new Option<string>(value);

            // Assert
            Assert.That(option.HasValue, Is.True);
            Assert.That(option.Value, Is.EqualTo(value));
        }

        [Test]
        public void Constructor_WithNullValue_CreatesNone()
        {
            // Arrange
            string? value = null;

            // Act
            var option = new Option<string>(value!);

            // Assert
            Assert.That(option.HasValue, Is.False);
        }

        [Test]
        public void Value_WhenHasValue_ReturnsValue()
        {
            // Arrange
            var option = new Option<int>(42);

            // Act & Assert
            Assert.That(option.Value, Is.EqualTo(42));
        }

        [Test]
        public void Value_WhenNone_ThrowsInvalidOperationException()
        {
            // Arrange
            var option = new Option<string>(null!);

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _ = option.Value);
        }

        [Test]
        public void Map_WhenSome_ReturnsMappedValue()
        {
            // Arrange
            var option = new Option<int>(10);
            Func<int, string> mapper = x => (x * 2).ToString();

            // Act
            var result = option.Map(mapper);

            // Assert
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo("20"));
        }

        [Test]
        public void Map_WhenNone_ReturnsNone()
        {
            // Arrange
            var option = new Option<string>(null!);
            Func<string, int> mapper = s => s.Length;

            // Act
            var result = option.Map(mapper);

            // Assert
            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void FlatMap_WhenSome_ReturnsMappedOption()
        {
            // Arrange
            var option = new Option<int>(5);
            Func<int, Option<string>> mapper = x => new Option<string>((x * 2).ToString());

            // Act
            var result = option.FlatMap(mapper);

            // Assert
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo("10"));
        }

        [Test]
        public void FlatMap_WhenNone_ReturnsNone()
        {
            // Arrange
            var option = new Option<string>(null!);
            Func<string, Option<int>> mapper = s => new Option<int>(s.Length);

            // Act
            var result = option.FlatMap(mapper);

            // Assert
            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void GetOrDefault_WhenSome_ReturnsValue()
        {
            // Arrange
            var option = new Option<string>("hello");

            // Act
            var result = option.GetOrDefault();

            // Assert
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void GetOrDefault_WhenNone_ReturnsDefault()
        {
            // Arrange
            var option = new Option<string>(null!);

            // Act
            var result = option.GetOrDefault();

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetOrDefault_WithDefaultValue_WhenSome_ReturnsValue()
        {
            // Arrange
            var option = new Option<int>(100);

            // Act
            var result = option.GetOrDefault(999);

            // Assert
            Assert.That(result, Is.EqualTo(100));
        }

        [Test]
        public void GetOrDefault_WithDefaultValue_WhenNone_ReturnsDefault()
        {
            // Arrange
            var option = new Option<int>();

            // Act
            var result = option.GetOrDefault(999);

            // Assert
            Assert.That(result, Is.EqualTo(999));
        }

        [Test]
        public void GetOrDefault_WithThunk_WhenSome_ReturnsValue()
        {
            // Arrange
            var option = new Option<string>("hello");
            Func<string> thunk = () => "default";

            // Act
            var result = option.GetOrDefault(thunk);

            // Assert
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void GetOrDefault_WithThunk_WhenNone_ReturnsThunkResult()
        {
            // Arrange
            var option = new Option<string>(null!);
            Func<string> thunk = () => "default";

            // Act
            var result = option.GetOrDefault(thunk);

            // Assert
            Assert.That(result, Is.EqualTo("default"));
        }

        [Test]
        public void OrElse_WhenSome_ReturnsThis()
        {
            // Arrange
            var option = new Option<int>(42);
            var alternative = new Option<int>(99);

            // Act
            var result = option.OrElse(alternative);

            // Assert
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
        }

        [Test]
        public void OrElse_WhenNone_ReturnsAlternative()
        {
            // Arrange
            var option = new Option<int>();
            var alternative = new Option<int>(99);

            // Act
            var result = option.OrElse(alternative);

            // Assert
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo(99));
        }

        [Test]
        public void OrElse_WithThunk_WhenSome_ReturnsThis()
        {
            // Arrange
            var option = new Option<string>("hello");
            Func<Option<string>> thunk = () => new Option<string>("world");

            // Act
            var result = option.OrElse(thunk);

            // Assert
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo("hello"));
        }

        [Test]
        public void OrElse_WithThunk_WhenNone_ReturnsThunkResult()
        {
            // Arrange
            var option = new Option<string>(null!);
            Func<Option<string>> thunk = () => new Option<string>("world");

            // Act
            var result = option.OrElse(thunk);

            // Assert
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo("world"));
        }

        [Test]
        public void Zip_WhenBothSome_ReturnsSomeTuple()
        {
            // Arrange
            var option1 = new Option<int>(10);
            var option2 = new Option<string>("hello");

            // Act
            var result = option1.Zip(option2);

            // Assert
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo((10, "hello")));
        }

        [Test]
        public void Zip_WhenFirstNone_ReturnsNone()
        {
            // Arrange
            var option1 = new Option<int>();
            var option2 = new Option<string>("hello");

            // Act
            var result = option1.Zip(option2);

            // Assert
            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void Zip_WhenSecondNone_ReturnsNone()
        {
            // Arrange
            var option1 = new Option<int>(10);
            var option2 = new Option<string>(null!);

            // Act
            var result = option1.Zip(option2);

            // Assert
            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void Deconstruct_WhenSome_ReturnsHasValueTrueAndValue()
        {
            // Arrange
            var option = new Option<int>(42);

            // Act
            var (hasValue, value) = option;

            // Assert
            Assert.That(hasValue, Is.True);
            Assert.That(value, Is.EqualTo(42));
        }

        [Test]
        public void Deconstruct_WhenNone_ReturnsHasValueFalseAndDefault()
        {
            // Arrange
            var option = new Option<int>();

            // Act
            var (hasValue, value) = option;

            // Assert
            Assert.That(hasValue, Is.False);
            Assert.That(value, Is.EqualTo(default(int)));
        }

        [Test]
        public void Where_WhenSomeAndPredicateTrue_ReturnsThis()
        {
            // Arrange
            var option = new Option<int>(42);
            Func<int, bool> predicate = x => x > 0;

            // Act
            var result = option.Where(predicate);

            // Assert
            Assert.That(result.HasValue, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
        }

        [Test]
        public void Where_WhenSomeAndPredicateFalse_ReturnsNone()
        {
            // Arrange
            var option = new Option<int>(42);
            Func<int, bool> predicate = x => x < 0;

            // Act
            var result = option.Where(predicate);

            // Assert
            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void Where_WhenNone_ReturnsNone()
        {
            // Arrange
            var option = new Option<int>();
            Func<int, bool> predicate = x => x > 0;

            // Act
            var result = option.Where(predicate);

            // Assert
            Assert.That(result.HasValue, Is.False);
        }

        [Test]
        public void Equals_WhenBothSomeWithSameValue_ReturnsTrue()
        {
            // Arrange
            var option1 = new Option<string>("hello");
            var option2 = new Option<string>("hello");

            // Act & Assert
            Assert.That(option1.Equals(option2), Is.True);
            Assert.That(option1 == option2, Is.True);
        }

        [Test]
        public void Equals_WhenBothSomeWithDifferentValues_ReturnsFalse()
        {
            // Arrange
            var option1 = new Option<string>("hello");
            var option2 = new Option<string>("world");

            // Act & Assert
            Assert.That(option1.Equals(option2), Is.False);
            Assert.That(option1 != option2, Is.True);
        }

        [Test]
        public void Equals_WhenBothNone_ReturnsTrue()
        {
            // Arrange
            var option1 = new Option<string>(null!);
            var option2 = new Option<string>(null!);

            // Act & Assert
            Assert.That(option1.Equals(option2), Is.True);
            Assert.That(option1 == option2, Is.True);
        }

        [Test]
        public void Equals_WhenOneSomeOneNone_ReturnsFalse()
        {
            // Arrange
            var option1 = new Option<string>("hello");
            var option2 = new Option<string>(null!);

            // Act & Assert
            Assert.That(option1.Equals(option2), Is.False);
            Assert.That(option1 != option2, Is.True);
        }

        [Test]
        public void GetHashCode_WhenSome_ReturnsValueHashCode()
        {
            // Arrange
            var value = "hello";
            var option = new Option<string>(value);

            // Act
            var hashCode = option.GetHashCode();

            // Assert
            Assert.That(hashCode, Is.EqualTo(value.GetHashCode()));
        }

        [Test]
        public void GetHashCode_WhenNone_ReturnsZero()
        {
            // Arrange
            var option = new Option<string>(null!);

            // Act
            var hashCode = option.GetHashCode();

            // Assert
            Assert.That(hashCode, Is.EqualTo(0));
        }

        [Test]
        public void ToString_WhenSome_ReturnsValueToString()
        {
            // Arrange
            var option = new Option<int>(42);

            // Act
            var str = option.ToString();

            // Assert
            Assert.That(str, Is.EqualTo("42"));
        }

        [Test]
        public void ToString_WhenNone_ReturnsNullOptionString()
        {
            // Arrange
            var option = new Option<int>();

            // Act
            var str = option.ToString();

            // Assert
            Assert.That(str, Is.EqualTo("<null option>"));
        }

        [Test]
        public void IEnumerable_WhenSome_EnumeratesSingleValue()
        {
            // Arrange
            var option = new Option<string>("hello");

            // Act & Assert
            int count = 0;
            foreach (var item in option)
            {
                Assert.That(item, Is.EqualTo("hello"));
                count++;
            }
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void IEnumerable_WhenNone_EnumeratesNothing()
        {
            // Arrange
            var option = new Option<string>(null!);

            // Act & Assert
            int count = 0;
            foreach (var item in option)
            {
                count++;
            }
            Assert.That(count, Is.EqualTo(0));
        }

        [Test]
        public void IReadOnlyCollection_Count_WhenSome_ReturnsOne()
        {
            // Arrange
            var option = new Option<int>(42);

            // Act & Assert
            Assert.That(((IReadOnlyCollection<int>)option).Count, Is.EqualTo(1));
        }

        [Test]
        public void IReadOnlyCollection_Count_WhenNone_ReturnsZero()
        {
            // Arrange
            var option = new Option<int>();

            // Act & Assert
            Assert.That(((IReadOnlyCollection<int>)option).Count, Is.EqualTo(0));
        }
    }
}