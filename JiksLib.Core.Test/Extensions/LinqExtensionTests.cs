using NUnit.Framework;
using JiksLib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Extensions
{
    [TestFixture]
    public class LinqExtensionTests
    {
        #region IsEmpty Tests

        [Test]
        public void IsEmpty_WithEmptySequence_ReturnsTrue()
        {
            // Arrange
            IEnumerable<int> emptySequence = Enumerable.Empty<int>();

            // Act
            bool result = emptySequence.IsEmpty();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsEmpty_WithNonEmptySequence_ReturnsFalse()
        {
            // Arrange
            IEnumerable<int> sequence = new[] { 1, 2, 3 };

            // Act
            bool result = sequence.IsEmpty();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsEmpty_WithNullElementSequence_ReturnsFalse()
        {
            // Arrange
            IEnumerable<string?> sequence = new[] { "a", null, "c" };

            // Act
            bool result = sequence.IsEmpty();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsEmpty_WithSingleElementSequence_ReturnsFalse()
        {
            // Arrange
            IEnumerable<int> sequence = new[] { 42 };

            // Act
            bool result = sequence.IsEmpty();

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsEmpty_WithLargeSequence_ReturnsFalse()
        {
            // Arrange
            IEnumerable<int> sequence = Enumerable.Range(1, 1000);

            // Act
            bool result = sequence.IsEmpty();

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region RandomSelect Tests

        [Test]
        public void RandomSelect_WithEmptySequence_ThrowsInvalidOperationException()
        {
            // Arrange
            IEnumerable<int> emptySequence = Enumerable.Empty<int>();
            float randomNumber = 0.5f;
            Func<int, float> getWeight = x => 1.0f;

            // Act & Assert
            Assert.That(
                () => emptySequence.RandomSelect(randomNumber, getWeight),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("ls cannot be empty."));
        }

        [Test]
        public void RandomSelect_WithSingleElement_AlwaysReturnsThatElement()
        {
            // Arrange
            IEnumerable<string> sequence = new[] { "only" };
            Func<string, float> getWeight = x => 1.0f;

            // Act & Assert
            for (float r = 0f; r <= 1f; r += 0.1f)
            {
                var result = sequence.RandomSelect(r, getWeight);
                Assert.That(result, Is.EqualTo("only"));
            }
        }

        [Test]
        public void RandomSelect_WithEqualWeights_ReturnsElementsAccordingToRandomNumber()
        {
            // Arrange
            IEnumerable<string> sequence = new[] { "A", "B", "C" };
            Func<string, float> getWeight = x => 1.0f;

            // Test different random numbers
            // With equal weights of 1 each, total weight = 3
            // Boundaries: [0,1) -> A, [1,2) -> B, [2,3] -> C

            // Act & Assert
            // randomNumber = 0 should select A (0 * 3 = 0)
            var result1 = sequence.RandomSelect(0f, getWeight);
            Assert.That(result1, Is.EqualTo("A"));

            // randomNumber = 0.33 should select A (0.33 * 3 = 0.99)
            var result2 = sequence.RandomSelect(0.33f, getWeight);
            Assert.That(result2, Is.EqualTo("A"));

            // randomNumber = 0.34 should select B (0.34 * 3 = 1.02)
            var result3 = sequence.RandomSelect(0.34f, getWeight);
            Assert.That(result3, Is.EqualTo("B"));

            // randomNumber = 0.66 should select B (0.66 * 3 = 1.98)
            var result4 = sequence.RandomSelect(0.66f, getWeight);
            Assert.That(result4, Is.EqualTo("B"));

            // randomNumber = 0.67 should select C (0.67 * 3 = 2.01)
            var result5 = sequence.RandomSelect(0.67f, getWeight);
            Assert.That(result5, Is.EqualTo("C"));

            // randomNumber = 1 should select C (1 * 3 = 3)
            var result6 = sequence.RandomSelect(1f, getWeight);
            Assert.That(result6, Is.EqualTo("C"));
        }

        [Test]
        public void RandomSelect_WithDifferentWeights_ReturnsElementsAccordingToWeightProportions()
        {
            // Arrange
            // Weights: A=1, B=2, C=3, total=6
            IEnumerable<string> sequence = new[] { "A", "B", "C" };
            Func<string, float> getWeight = x => x switch
            {
                "A" => 1f,
                "B" => 2f,
                "C" => 3f,
                _ => 0f
            };

            // Boundaries:
            // A: [0,1) -> weight 1
            // B: [1,3) -> weight 2
            // C: [3,6] -> weight 3

            // Act & Assert
            // randomNumber = 0 should select A
            var result1 = sequence.RandomSelect(0f, getWeight);
            Assert.That(result1, Is.EqualTo("A"));

            // randomNumber = 0.16 should select A (0.16 * 6 = 0.96)
            var result2 = sequence.RandomSelect(0.16f, getWeight);
            Assert.That(result2, Is.EqualTo("A"));

            // randomNumber = 0.17 should select B (0.17 * 6 = 1.02)
            var result3 = sequence.RandomSelect(0.17f, getWeight);
            Assert.That(result3, Is.EqualTo("B"));

            // randomNumber = 0.5 should select B (0.5 * 6 = 3.0, boundary of B)
            var result4 = sequence.RandomSelect(0.5f, getWeight);
            Assert.That(result4, Is.EqualTo("B"));

            // randomNumber = 0.51 should select C (0.51 * 6 = 3.06)
            var result5 = sequence.RandomSelect(0.51f, getWeight);
            Assert.That(result5, Is.EqualTo("C"));

            // randomNumber = 1 should select C
            var result6 = sequence.RandomSelect(1f, getWeight);
            Assert.That(result6, Is.EqualTo("C"));
        }

        [Test]
        public void RandomSelect_WithZeroWeights_ReturnsFirstElement()
        {
            // Arrange
            IEnumerable<string> sequence = new[] { "A", "B", "C" };
            Func<string, float> getWeight = x => 0f; // All weights zero

            // Act & Assert
            // With all weights zero, total weight = 0, selectedWeight = 0
            // Algorithm should return first element
            var result = sequence.RandomSelect(0.5f, getWeight);
            Assert.That(result, Is.EqualTo("A"));
        }

        [Test]
        public void RandomSelect_WithRandomNumberOutsideZeroToOne_StillWorks()
        {
            // Arrange
            // Method doesn't validate randomNumber range
            IEnumerable<string> sequence = new[] { "A", "B" };
            Func<string, float> getWeight = x => 1f;

            // Act & Assert
            // randomNumber = -0.5, total weight = 2, selectedWeight = -1
            // Algorithm: selectedWeight <= p? -1 <= 1 true, returns A
            var result1 = sequence.RandomSelect(-0.5f, getWeight);
            Assert.That(result1, Is.EqualTo("A"));

            // randomNumber = 1.5, total weight = 2, selectedWeight = 3
            // Algorithm: selectedWeight <= p? 3 <= 1 false, subtract 1 => 2
            // Next element: 2 <= 1 false, subtract 1 => 1
            // Returns last element B
            var result2 = sequence.RandomSelect(1.5f, getWeight);
            Assert.That(result2, Is.EqualTo("B"));
        }

        #endregion
    }
}