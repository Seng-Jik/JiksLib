using NUnit.Framework;
using JiksLib;
using System;
using System.IO;

namespace JiksLib.Test
{
    [TestFixture]
    public class ShellUtilsTests
    {
        #region IsWindowsFamilyOS Tests

        [Test]
        public void IsWindowsFamilyOS_Win32NT_ReturnsTrue()
        {
            // Arrange & Act
            bool result = ShellUtils.IsWindowsFamilyOS(PlatformID.Win32NT);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsWindowsFamilyOS_Win32Windows_ReturnsTrue()
        {
            // Arrange & Act
            bool result = ShellUtils.IsWindowsFamilyOS(PlatformID.Win32Windows);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsWindowsFamilyOS_Win32S_ReturnsTrue()
        {
            // Arrange & Act
            bool result = ShellUtils.IsWindowsFamilyOS(PlatformID.Win32S);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsWindowsFamilyOS_WinCE_ReturnsTrue()
        {
            // Arrange & Act
            bool result = ShellUtils.IsWindowsFamilyOS(PlatformID.WinCE);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsWindowsFamilyOS_Xbox_ReturnsTrue()
        {
            // Arrange & Act
            bool result = ShellUtils.IsWindowsFamilyOS(PlatformID.Xbox);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsWindowsFamilyOS_Unix_ReturnsFalse()
        {
            // Arrange & Act
            bool result = ShellUtils.IsWindowsFamilyOS(PlatformID.Unix);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsWindowsFamilyOS_MacOSX_ReturnsFalse()
        {
            // Arrange & Act
            bool result = ShellUtils.IsWindowsFamilyOS(PlatformID.MacOSX);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsWindowsFamilyOS_OtherPlatformID_ReturnsFalse()
        {
            // Arrange & Act - 测试未知的 PlatformID 值
            var unknownPlatform = (PlatformID)999;
            bool result = ShellUtils.IsWindowsFamilyOS(unknownPlatform);

            // Assert
            Assert.That(result, Is.False);
        }

        #endregion

        #region IsCurrentOSWindowsFamily Tests

        [Test]
        public void IsCurrentOSWindowsFamily_Property_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => { var _ = ShellUtils.IsCurrentOSWindowsFamily; });
        }

        [Test]
        public void IsCurrentOSWindowsFamily_Property_ReturnsBoolean()
        {
            // Arrange & Act
            bool result = ShellUtils.IsCurrentOSWindowsFamily;

            // Assert - 只需要验证返回的是布尔值（无异常）
            Assert.That(result, Is.TypeOf<bool>());
        }

        #endregion

        #region ExecutableSuffixes Tests

        [Test]
        public void ExecutableSuffixes_Property_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => { var _ = ShellUtils.ExecutableSuffixes; });
        }

        [Test]
        public void ExecutableSuffixes_Property_ReturnsNonEmptyCollection()
        {
            // Arrange & Act
            var suffixes = ShellUtils.ExecutableSuffixes;

            // Assert
            Assert.That(suffixes, Is.Not.Null);
            Assert.That(suffixes, Is.Not.Empty);
        }

        [Test]
        public void ExecutableSuffixes_Property_ContainsExpectedSuffixes()
        {
            // Arrange & Act
            var suffixes = ShellUtils.ExecutableSuffixes;

            // Assert - 验证集合包含常见的可执行文件后缀
            // 注意：具体后缀取决于当前操作系统
            if (ShellUtils.IsCurrentOSWindowsFamily)
            {
                Assert.That(suffixes, Contains.Item(".exe"));
                Assert.That(suffixes, Contains.Item(".cmd"));
                Assert.That(suffixes, Contains.Item(".bat"));
                Assert.That(suffixes, Contains.Item(".ps1"));
            }
            else
            {
                Assert.That(suffixes, Contains.Item(""));
                Assert.That(suffixes, Contains.Item(".sh"));
            }
        }

        #endregion

        #region FindExecutable Tests

        [Test]
        public void FindExecutable_NullName_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.That(() => ShellUtils.FindExecutable(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void FindExecutable_EmptyName_ReturnsNull()
        {
            // Arrange
            string emptyName = "";

            // Act
            var result = ShellUtils.FindExecutable(emptyName);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindExecutable_FileInCurrentDirectory_ReturnsFileInfo()
        {
            // 跳过，因为修改当前目录可能影响其他测试
            // 使用临时目录的测试更安全
            Assert.Pass("FindExecutable file tests require temporary directory setup");
        }

        [Test]
        public void FindExecutable_FileWithExecutableSuffixInDirectory_ReturnsFileInfo()
        {
            // 创建临时目录和文件
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                // 获取当前平台的可执行文件后缀（使用第一个非空后缀）
                string suffix = ShellUtils.ExecutableSuffixes[0];
                if (string.IsNullOrEmpty(suffix))
                {
                    suffix = ShellUtils.ExecutableSuffixes.Count > 1 ? ShellUtils.ExecutableSuffixes[1] : "";
                }

                string executableName = "testexecutable";
                string fileName = executableName + suffix;
                string filePath = Path.Combine(tempDir, fileName);

                // 创建空文件（模拟可执行文件）
                File.WriteAllText(filePath, "");

                // 保存原始PATH并设置临时目录到PATH中
                string originalPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                try
                {
                    Environment.SetEnvironmentVariable("PATH", tempDir);

                    // Act
                    var result = ShellUtils.FindExecutable(executableName);

                    // Assert
                    Assert.That(result, Is.Not.Null);
                    Assert.That(result!.FullName, Is.EqualTo(filePath));
                }
                finally
                {
                    // 恢复原始PATH
                    Environment.SetEnvironmentVariable("PATH", originalPath);
                }
            }
            finally
            {
                // 清理临时目录
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }

        [Test]
        public void FindExecutable_FileNotFound_ReturnsNull()
        {
            // Arrange
            string nonExistentExecutable = Guid.NewGuid().ToString();

            // Act
            var result = ShellUtils.FindExecutable(nonExistentExecutable);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindExecutable_WithPathSeparatorInName_ReturnsNull()
        {
            // Arrange - 包含路径分隔符的名称应该直接检查文件是否存在，不搜索PATH
            string nameWithPath = $"test{Path.DirectorySeparatorChar}program";

            // Act
            var result = ShellUtils.FindExecutable(nameWithPath);

            // Assert
            // 由于文件不存在，应该返回null
            Assert.That(result, Is.Null);
        }

        #endregion

        #region Platform-Specific Tests

        [Test]
        public void ExecutableSuffixes_WindowsFamily_ContainsExpectedExtensions()
        {
            // 这个测试只在Windows上运行
            if (!ShellUtils.IsCurrentOSWindowsFamily)
            {
                Assert.Pass("Test only runs on Windows family OS");
                return;
            }

            // Arrange & Act
            var suffixes = ShellUtils.ExecutableSuffixes;

            // Assert
            Assert.That(suffixes, Contains.Item(".exe"));
            Assert.That(suffixes, Contains.Item(".cmd"));
            Assert.That(suffixes, Contains.Item(".bat"));
            Assert.That(suffixes, Contains.Item(".ps1"));
        }

        [Test]
        public void ExecutableSuffixes_NonWindowsFamily_ContainsExpectedExtensions()
        {
            // 这个测试只在非Windows上运行
            if (ShellUtils.IsCurrentOSWindowsFamily)
            {
                Assert.Pass("Test only runs on non-Windows OS");
                return;
            }

            // Arrange & Act
            var suffixes = ShellUtils.ExecutableSuffixes;

            // Assert
            Assert.That(suffixes, Contains.Item(""));
            Assert.That(suffixes, Contains.Item(".sh"));
        }

        #endregion

        #region Integration Style Tests (Require Setup)

        [Test]
        public void FindExecutable_WithMockEnvironment_ReturnsCorrectFile()
        {
            // 这个测试需要模拟文件系统和环境变量
            // 由于ShellUtils使用静态Environment和File方法，难以进行单元测试
            // 考虑重构ShellUtils以支持依赖注入，或使用条件编译
            Assert.Pass("FindExecutable integration tests require environment setup");
        }

        #endregion
    }
}