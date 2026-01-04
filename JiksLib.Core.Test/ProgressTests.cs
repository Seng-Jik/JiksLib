using NUnit.Framework;
using JiksLib;
using System;
using System.Collections.Generic;

namespace JiksLib.Test
{
    [TestFixture]
    public class ProgressTests
    {
        #region Null Tests

        [Test]
        public void Null_WithAnyType_ReturnsNonNullInstance()
        {
            // Arrange & Act
            var progressInt = Progress.Null<int>();
            var progressString = Progress.Null<string>();
            var progressObject = Progress.Null<object>();

            // Assert
            Assert.That(progressInt, Is.Not.Null);
            Assert.That(progressString, Is.Not.Null);
            Assert.That(progressObject, Is.Not.Null);
        }

        [Test]
        public void Null_Report_DoesNothing()
        {
            // Arrange
            var progress = Progress.Null<string>();
            bool wasCalled = false;

            // This test ensures Report doesn't throw
            // We can't directly verify "nothing happens" but we can verify no exception
            // Act & Assert
            Assert.DoesNotThrow(() => progress.Report("test"));
            Assert.DoesNotThrow(() => progress.Report(null));
            Assert.DoesNotThrow(() => progress.Report(""));
        }

        [Test]
        public void Null_IsSingletonPerType()
        {
            // Arrange & Act
            var instance1 = Progress.Null<int>();
            var instance2 = Progress.Null<int>();
            var instance3 = Progress.Null<string>();

            // Assert
            Assert.That(instance1, Is.SameAs(instance2));
            Assert.That(instance1, Is.Not.SameAs(instance3)); // Different type should be different instance
        }

        #endregion

        #region Create Tests

        [Test]
        public void Create_WithNullAction_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.That(() => Progress.Create<int>(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Create_WithAction_ReportsValue()
        {
            // Arrange
            int reportedValue = 0;
            Action<int> onProgress = x => reportedValue = x;
            var progress = Progress.Create(onProgress);

            // Act
            progress.Report(42);

            // Assert
            Assert.That(reportedValue, Is.EqualTo(42));
        }

        [Test]
        public void Create_ReportsMultipleTimes()
        {
            // Arrange
            var reportedValues = new List<string>();
            Action<string> onProgress = x => reportedValues.Add(x);
            var progress = Progress.Create(onProgress);

            // Act
            progress.Report("first");
            progress.Report("second");
            progress.Report("third");

            // Assert
            Assert.That(reportedValues, Is.EqualTo(new[] { "first", "second", "third" }));
        }

        [Test]
        public void Create_WithReferenceType_ReportsSameReference()
        {
            // Arrange
            object originalObject = new object();
            object reportedObject = null!;
            Action<object> onProgress = x => reportedObject = x;
            var progress = Progress.Create(onProgress);

            // Act
            progress.Report(originalObject);

            // Assert
            Assert.That(reportedObject, Is.SameAs(originalObject));
        }

        [Test]
        public void Create_WithValueType_ReportsValue()
        {
            // Arrange
            DateTime? reportedValue = null;
            Action<DateTime> onProgress = x => reportedValue = x;
            var progress = Progress.Create(onProgress);
            var testDate = new DateTime(2023, 1, 1);

            // Act
            progress.Report(testDate);

            // Assert
            Assert.That(reportedValue, Is.EqualTo(testDate));
        }

        #endregion

        #region CreateSubProgress Tests

        [Test]
        public void CreateSubProgress_WithNullParent_ThrowsArgumentNullException()
        {
            // Arrange
            IProgress<float> nullParent = null!;

            // Act & Assert
            Assert.That(() => nullParent.CreateSubProgress(0f, 1f), Throws.ArgumentNullException);
        }

        [Test]
        public void CreateSubProgress_WithNullProgressParent_ReturnsNullProgress()
        {
            // Arrange
            var nullParent = Progress.Null<float>();

            // Act
            var subProgress = nullParent.CreateSubProgress(0f, 1f);

            // Assert
            Assert.That(subProgress, Is.SameAs(Progress.Null<float>()));
        }

        [Test]
        public void CreateSubProgress_WithNormalParent_ReportsCorrectMapping()
        {
            // Arrange
            float reportedValue = 0f;
            Action<float> onProgress = x => reportedValue = x;
            var parentProgress = Progress.Create(onProgress);
            var subProgress = parentProgress.CreateSubProgress(0.2f, 0.8f);

            // Act & Assert - Test boundary cases
            subProgress.Report(0f);
            Assert.That(reportedValue, Is.EqualTo(0.2f).Within(0.0001f));

            subProgress.Report(0.5f);
            Assert.That(reportedValue, Is.EqualTo(0.5f).Within(0.0001f)); // 0.2 + 0.5*(0.8-0.2) = 0.5

            subProgress.Report(1f);
            Assert.That(reportedValue, Is.EqualTo(0.8f).Within(0.0001f));
        }

        [Test]
        public void CreateSubProgress_WithReverseRange_ReportsCorrectMapping()
        {
            // Arrange
            float reportedValue = 0f;
            Action<float> onProgress = x => reportedValue = x;
            var parentProgress = Progress.Create(onProgress);
            var subProgress = parentProgress.CreateSubProgress(0.8f, 0.2f); // Start > End

            // Act & Assert
            subProgress.Report(0f);
            Assert.That(reportedValue, Is.EqualTo(0.8f).Within(0.0001f));

            subProgress.Report(0.5f);
            Assert.That(reportedValue, Is.EqualTo(0.5f).Within(0.0001f)); // 0.8 + 0.5*(0.2-0.8) = 0.5

            subProgress.Report(1f);
            Assert.That(reportedValue, Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void CreateSubProgress_WithSameStartAndEnd_ReportsConstantValue()
        {
            // Arrange
            float reportedValue = 0f;
            Action<float> onProgress = x => reportedValue = x;
            var parentProgress = Progress.Create(onProgress);
            var subProgress = parentProgress.CreateSubProgress(0.5f, 0.5f);

            // Act & Assert
            subProgress.Report(0f);
            Assert.That(reportedValue, Is.EqualTo(0.5f).Within(0.0001f));

            subProgress.Report(0.3f);
            Assert.That(reportedValue, Is.EqualTo(0.5f).Within(0.0001f));

            subProgress.Report(1f);
            Assert.That(reportedValue, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void CreateSubProgress_WithNegativeProgress_ReportsCorrectMapping()
        {
            // Arrange
            float reportedValue = 0f;
            Action<float> onProgress = x => reportedValue = x;
            var parentProgress = Progress.Create(onProgress);
            var subProgress = parentProgress.CreateSubProgress(-0.5f, 0.5f);

            // Act & Assert
            subProgress.Report(0f);
            Assert.That(reportedValue, Is.EqualTo(-0.5f).Within(0.0001f));

            subProgress.Report(0.5f);
            Assert.That(reportedValue, Is.EqualTo(0f).Within(0.0001f)); // -0.5 + 0.5*(0.5 - (-0.5)) = 0

            subProgress.Report(1f);
            Assert.That(reportedValue, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void CreateSubProgress_CanBeChained()
        {
            // Arrange
            float reportedValue = 0f;
            Action<float> onProgress = x => reportedValue = x;
            var parentProgress = Progress.Create(onProgress);
            var subProgress1 = parentProgress.CreateSubProgress(0f, 0.5f);
            var subProgress2 = subProgress1.CreateSubProgress(0f, 1f);

            // Act & Assert
            subProgress2.Report(0.5f);
            // subProgress2: 0.5 maps to subProgress1: 0.5
            // subProgress1: 0.5 maps to parent: 0 + 0.5*(0.5-0) = 0.25
            Assert.That(reportedValue, Is.EqualTo(0.25f).Within(0.0001f));
        }

        #endregion

        #region Convert Tests

        [Test]
        public void Convert_WithNullSource_ThrowsArgumentNullException()
        {
            // Arrange
            IProgress<int> nullSource = null!;
            Func<string, int> converter = x => x.Length;

            // Act & Assert
            Assert.That(() => nullSource.Convert(converter), Throws.ArgumentNullException);
        }

        [Test]
        public void Convert_WithNullConverter_ThrowsArgumentNullException()
        {
            // Arrange
            var source = Progress.Create<int>(x => { });
            Func<string, int> nullConverter = null!;

            // Act & Assert
            Assert.That(() => source.Convert(nullConverter), Throws.ArgumentNullException);
        }

        [Test]
        public void Convert_WithNullProgressSource_ReturnsNullProgress()
        {
            // Arrange
            var nullSource = Progress.Null<int>();
            Func<string, int> converter = x => x.Length;

            // Act
            var converted = nullSource.Convert(converter);

            // Assert
            Assert.That(converted, Is.SameAs(Progress.Null<string>()));
        }

        [Test]
        public void Convert_WithNormalSource_ReportsConvertedValue()
        {
            // Arrange
            string reportedValue = null!;
            Action<string> onProgress = x => reportedValue = x;
            var source = Progress.Create(onProgress);
            Func<int, string> converter = x => $"Value: {x}";
            var converted = source.Convert(converter);

            // Act
            converted.Report(42);

            // Assert
            Assert.That(reportedValue, Is.EqualTo("Value: 42"));
        }

        [Test]
        public void Convert_PreservesMultipleReports()
        {
            // Arrange
            var reportedValues = new List<int>();
            Action<int> onProgress = x => reportedValues.Add(x);
            var source = Progress.Create(onProgress);
            Func<string, int> converter = x => x.Length;
            var converted = source.Convert(converter);

            // Act
            converted.Report("a");
            converted.Report("ab");
            converted.Report("abc");

            // Assert
            Assert.That(reportedValues, Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void Convert_WithComplexConversion_WorksCorrectly()
        {
            // Arrange
            DateTime? reportedValue = null;
            Action<DateTime> onProgress = x => reportedValue = x;
            var source = Progress.Create(onProgress);
            Func<string, DateTime> converter = x => DateTime.Parse(x);
            var converted = source.Convert(converter);
            var testDate = new DateTime(2023, 12, 31);

            // Act
            converted.Report("2023-12-31");

            // Assert
            Assert.That(reportedValue, Is.EqualTo(testDate));
        }

        [Test]
        public void Convert_CanBeChained()
        {
            // Arrange
            int reportedValue = 0;
            Action<int> onProgress = x => reportedValue = x;
            var source = Progress.Create(onProgress);

            // Chain: string -> int -> string -> int
            var converted1 = source.Convert((string s) => s.Length); // string -> int
            var converted2 = converted1.Convert((int i) => i.ToString()); // int -> string
            var converted3 = converted2.Convert((string s) => s.Length); // string -> int

            // Act
            converted3.Report("hello");

            // Assert
            // "hello".Length = 5 -> "5".Length = 1
            Assert.That(reportedValue, Is.EqualTo(1));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Integration_NullProgressWithSubProgress_ReturnsNullProgress()
        {
            // Arrange
            var nullProgress = Progress.Null<float>();

            // Act
            var subProgress = nullProgress.CreateSubProgress(0f, 1f);

            // Assert
            Assert.That(subProgress, Is.SameAs(Progress.Null<float>()));
            // Verify it's truly null by checking no exception on Report
            Assert.DoesNotThrow(() => subProgress.Report(0.5f));
        }

        [Test]
        public void Integration_SubProgressWithConvert_WorksCorrectly()
        {
            // Arrange
            float reportedValue = 0f;
            Action<float> onProgress = x => reportedValue = x;
            var parentProgress = Progress.Create(onProgress);

            // Create a sub-progress that reports 0-1 range
            var subProgress = parentProgress.CreateSubProgress(0.2f, 0.8f);

            // Convert float sub-progress to report string lengths
            var converted = subProgress.Convert((string s) => s.Length / 10f);

            // Act
            converted.Report("hello"); // length=5, progress=0.5

            // Assert
            // String length: 5 -> 5/10 = 0.5
            // Sub-progress mapping: 0.2 + 0.5*(0.8-0.2) = 0.5
            Assert.That(reportedValue, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void Integration_ComplexPipeline_WorksCorrectly()
        {
            // Arrange
            var reportedValues = new List<float>();
            Action<float> onProgress = x => reportedValues.Add(x);
            var rootProgress = Progress.Create(onProgress);

            // Create three parallel sub-progresses
            var subProgress1 = rootProgress.CreateSubProgress(0f, 0.33f);
            var subProgress2 = rootProgress.CreateSubProgress(0.33f, 0.66f);
            var subProgress3 = rootProgress.CreateSubProgress(0.66f, 1f);

            // Act
            subProgress1.Report(0.5f); // Should report: 0 + 0.5*(0.33-0) = 0.165
            subProgress2.Report(0.5f); // Should report: 0.33 + 0.5*(0.66-0.33) = 0.495
            subProgress3.Report(0.5f); // Should report: 0.66 + 0.5*(1-0.66) = 0.83

            // Assert
            Assert.That(reportedValues.Count, Is.EqualTo(3));
            Assert.That(reportedValues[0], Is.EqualTo(0.165f).Within(0.0001f));
            Assert.That(reportedValues[1], Is.EqualTo(0.495f).Within(0.0001f));
            Assert.That(reportedValues[2], Is.EqualTo(0.83f).Within(0.0001f));
        }

        #endregion
    }
}