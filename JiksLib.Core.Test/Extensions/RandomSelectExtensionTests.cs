using NUnit.Framework;
using JiksLib.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JiksLib.Test.Extensions
{
    [TestFixture]
    public class RandomSelectExtensionTests
    {
        #region RandomSelect Tests

        [Test]
        public void RandomSelect_WithEmptyList_ThrowsInvalidOperationException()
        {
            // Arrange
            IReadOnlyList<int> emptyList = new List<int>();
            float randomNumber = 0.5f;
            Func<int, float> getWeight = x => 1f;

            // Act & Assert
            Assert.That(
                () => emptyList.RandomSelect(randomNumber, getWeight),
                Throws.TypeOf<InvalidOperationException>()
                    .With.Message.Contains("ls cannot be empty."));
        }

        [Test]
        public void RandomSelect_WithSingleElementList_AlwaysReturnsThatElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "only" };
            Func<string, float> getWeight = x => 1f;

            // Act & Assert
            foreach (float r in new[] { 0f, 0.1f, 0.5f, 0.9f, 1f })
            {
                var result = list.RandomSelect(r, getWeight);
                Assert.That(result, Is.EqualTo("only"));
            }
        }

        [Test]
        public void RandomSelect_WithEqualWeights_AtZero_ReturnsFirstElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C", "D", "E" };
            float randomNumber = 0f;
            Func<string, float> getWeight = x => 1f;

            // Act
            var result = list.RandomSelect(randomNumber, getWeight);

            // Assert
            Assert.That(result, Is.EqualTo("A"));
        }

        [Test]
        public void RandomSelect_WithEqualWeights_AtOne_ReturnsLastElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C", "D", "E" };
            float randomNumber = 1f;
            Func<string, float> getWeight = x => 1f;

            // Act
            var result = list.RandomSelect(randomNumber, getWeight);

            // Assert
            Assert.That(result, Is.EqualTo("E"));
        }

        [Test]
        public void RandomSelect_WithEqualWeights_AtMidValue_ReturnsCorrectElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C", "D", "E" };
            // With equal weights, randomNumber maps linearly to elements
            // randomNumber = 0.5 -> selectedWeight = totalWeight * 0.5 = 5 * 0.5 = 2.5
            // Loop: i=0, p=1, selectedWeight <= 1? 2.5 <= 1 false, selectedWeight=1.5
            // i=1, p=1, 1.5 <= 1 false, selectedWeight=0.5
            // i=2, p=1, 0.5 <= 1 true, return C
            float randomNumber = 0.5f;
            Func<string, float> getWeight = x => 1f;

            // Act
            var result = list.RandomSelect(randomNumber, getWeight);

            // Assert
            Assert.That(result, Is.EqualTo("C"));
        }

        [Test]
        public void RandomSelect_WithUnequalWeights_ReturnsCorrectElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C" };
            // Weights: A=1, B=2, C=3, total=6
            Func<string, float> getWeight = x => x switch
            {
                "A" => 1f,
                "B" => 2f,
                "C" => 3f,
                _ => 0f
            };

            // Test cases: (randomNumber, expectedElement)
            var testCases = new[]
            {
                (0f, "A"),        // selectedWeight = 0, returns A
                (0.166f, "A"),    // selectedWeight = 1, <=1 returns A
                (0.167f, "B"),    // selectedWeight = 1.002, >1, subtract 1 -> 0.002, <=2 returns B
                (0.5f, "B"),      // selectedWeight = 3, >1, subtract 1 ->2, <=2 returns B
                (0.833f, "C"),    // selectedWeight = 5, >1, subtract1->4, >2, subtract2->2, <=3 returns C
                (1f, "C"),        // selectedWeight = 6, >1, subtract1->5, >2, subtract2->3, <=3 returns C
            };

            foreach (var (randomNumber, expectedElement) in testCases)
            {
                // Act
                var result = list.RandomSelect(randomNumber, getWeight);

                // Assert
                Assert.That(result, Is.EqualTo(expectedElement),
                    $"Failed for randomNumber={randomNumber}: expected {expectedElement}, got {result}");
            }
        }

        [Test]
        public void RandomSelect_WithAllZeroWeights_ReturnsFirstElement()
        {
            // Arrange
            IReadOnlyList<int> list = new List<int> { 10, 20, 30 };
            float randomNumber = 0.5f;
            Func<int, float> getWeight = x => 0f;

            // Act
            var result = list.RandomSelect(randomNumber, getWeight);

            // Assert
            // When all weights are zero, totalWeight = 0, selectedWeight = 0
            // First element satisfies selectedWeight <= p (0 <= 0)
            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void RandomSelect_WithNegativeWeight_ReturnsFirstElement()
        {
            // Arrange
            IReadOnlyList<int> list = new List<int> { 10, 20, 30 };
            float randomNumber = 0.5f;
            Func<int, float> getWeight = x => -1f; // Negative weight

            // Act
            var result = list.RandomSelect(randomNumber, getWeight);

            // Assert
            // totalWeight = -3, selectedWeight = -1.5
            // selectedWeight <= p (negative) will be true for first element
            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void RandomSelect_WithRandomNumberNegative_ReturnsFirstElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C" };
            float randomNumber = -0.5f;
            Func<string, float> getWeight = x => 1f;

            // Act
            var result = list.RandomSelect(randomNumber, getWeight);

            // Assert
            // selectedWeight = totalWeight * (-0.5) = 3 * -0.5 = -1.5
            // selectedWeight <= p (negative) true for first element
            Assert.That(result, Is.EqualTo("A"));
        }

        [Test]
        public void RandomSelect_WithRandomNumberGreaterThanOne_ReturnsLastElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C" };
            float randomNumber = 1.5f;
            Func<string, float> getWeight = x => 1f;

            // Act
            var result = list.RandomSelect(randomNumber, getWeight);

            // Assert
            // selectedWeight = totalWeight * 1.5 = 3 * 1.5 = 4.5
            // Loop: i=0, p=1, 4.5 <=1 false, selectedWeight=3.5
            // i=1, p=1, 3.5 <=1 false, selectedWeight=2.5
            // i=2, p=1, 2.5 <=1 false, selectedWeight=1.5
            // Loop ends, returns last element (C)
            Assert.That(result, Is.EqualTo("C"));
        }

        [Test]
        public void RandomSelect_WithFloatingPointPrecisionEdgeCase()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C" };
            // Weights: 1, 1, 1
            Func<string, float> getWeight = x => 1f;
            // randomNumber very close to 1 but not exactly 1
            float randomNumber = 0.9999999f; // totalWeight=3, selectedWeight=2.9999997
            // Loop: i=0, p=1, 2.9999997 <=1 false, selectedWeight=1.9999997
            // i=1, p=1, 1.9999997 <=1 false, selectedWeight=0.9999997
            // i=2, p=1, 0.9999997 <=1 true, return C

            // Act
            var result = list.RandomSelect(randomNumber, getWeight);

            // Assert
            Assert.That(result, Is.EqualTo("C"));
        }

        [Test]
        public void RandomSelect_WithExactlyOneMinusEpsilon()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B" };
            Func<string, float> getWeight = x => 1f;
            float randomNumber = 0.99999994f; // Close to 1 in single precision

            // Act
            var result = list.RandomSelect(randomNumber, getWeight);

            // Assert
            // selectedWeight = totalWeight * randomNumber = 2 * 0.99999994 = 1.99999988
            // i=0, p=1, 1.99999988 <=1 false, selectedWeight=0.99999988
            // i=1, p=1, 0.99999988 <=1 true, return B
            Assert.That(result, Is.EqualTo("B"));
        }

        [Test]
        public void RandomSelect_WithLargeList_WorksCorrectly()
        {
            // Arrange
            var largeList = Enumerable.Range(1, 1000).ToList();
            IReadOnlyList<int> list = largeList;
            // Weight proportional to value
            Func<int, float> getWeight = x => x;
            float randomNumber = 0.75f;

            // Act
            var result = list.RandomSelect(randomNumber, getWeight);

            // Assert
            // Since weights increase linearly, higher values have higher probability
            // With randomNumber=0.75, we expect a relatively high value
            Assert.That(result, Is.GreaterThanOrEqualTo(1));
            Assert.That(result, Is.LessThanOrEqualTo(1000));
            // Additional check: verify the algorithm would select this element
            // by manually computing selectedWeight
            float totalWeight = list.Sum(getWeight);
            float selectedWeight = totalWeight * randomNumber;
            float cumulative = 0;
            for (int i = 0; i < list.Count; i++)
            {
                float weight = getWeight(list[i]);
                if (selectedWeight <= cumulative + weight)
                {
                    Assert.That(result, Is.EqualTo(list[i]));
                    break;
                }
                cumulative += weight;
            }
        }

        [Test]
        public void RandomSelect_WithRandomNumberZeroAndZeroWeights_ReturnsFirstElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C" };
            float randomNumber = 0f;
            Func<string, float> getWeight = x => 0f;

            // Act
            var result = list.RandomSelect(randomNumber, getWeight);

            // Assert
            // totalWeight = 0, selectedWeight = 0
            // First element satisfies selectedWeight <= p (0 <= 0)
            Assert.That(result, Is.EqualTo("A"));
        }

        [Test]
        public void RandomSelect_WithRandomNumberOneAndZeroWeights_ReturnsFirstElement()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C" };
            float randomNumber = 1f;
            Func<string, float> getWeight = x => 0f;

            // Act
            var result = list.RandomSelect(randomNumber, getWeight);

            // Assert
            // totalWeight = 0, selectedWeight = 0
            // First element satisfies selectedWeight <= p (0 <= 0)
            Assert.That(result, Is.EqualTo("A"));
        }

        [Test]
        public void RandomSelect_WithMixedPositiveAndZeroWeights_WorksCorrectly()
        {
            // Arrange
            IReadOnlyList<string> list = new List<string> { "A", "B", "C", "D" };
            // Weights: A=0, B=2, C=0, D=3, total=5
            Func<string, float> getWeight = x => x switch
            {
                "A" => 0f,
                "B" => 2f,
                "C" => 0f,
                "D" => 3f,
                _ => 0f
            };

            // Test cases: (randomNumber, expectedElement)
            var testCases = new[]
            {
                (0f, "A"),        // selectedWeight = 0, returns A (zero weight but first)
                (0.001f, "B"),    // selectedWeight = 0.005, <=0? false (skip A), <=2? true, returns B
                (0.4f, "B"),      // selectedWeight = 2, <=2? true, returns B
                (0.8f, "D"),      // selectedWeight = 4, >2, subtract2->2, >0, subtract0->2, <=3 returns D
                (1f, "D"),        // selectedWeight = 5, >2, subtract2->3, >0, subtract0->3, <=3 returns D
            };

            foreach (var (randomNumber, expectedElement) in testCases)
            {
                // Act
                var result = list.RandomSelect(randomNumber, getWeight);

                // Assert
                Assert.That(result, Is.EqualTo(expectedElement),
                    $"Failed for randomNumber={randomNumber}: expected {expectedElement}, got {result}");
            }
        }

        #endregion
    }
}