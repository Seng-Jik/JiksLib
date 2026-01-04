using System;
using System.Collections.Generic;
using System.IO;
using JiksLib.Extensions;

namespace JiksLib
{
    /// <summary>
    /// Shell 工具
    /// </summary>
    public static class ShellUtils
    {
        /// <summary>
        /// 给定平台是否属于 Windows 家族
        /// </summary>
        /// <param name="id">平台ID</param>
        /// <returns>是否属于 Windows 家族</returns>
        public static bool IsWindowsFamilyOS(PlatformID id) => id switch
        {
            PlatformID.Win32S => true,
            PlatformID.Win32Windows => true,
            PlatformID.Win32NT => true,
            PlatformID.WinCE => true,
            PlatformID.Unix => false,
            PlatformID.Xbox => true,
            PlatformID.MacOSX => false,
            _ => false
        };

        /// <summary>
        /// 当前平台是否为 Windows 家族
        /// </summary>
        public static readonly bool IsCurrentOSWindowsFamily =
            IsWindowsFamilyOS(Environment.OSVersion.Platform);

        /// <summary>
        /// 当前平台下的可执行文件后缀
        /// </summary>
        public readonly static IReadOnlyList<string> ExecutableSuffixes =
            IsCurrentOSWindowsFamily
                ? new string[] { ".exe", ".cmd", ".bat", ".ps1" }
                : new string[] { "", ".sh" };

        /// <summary>
        /// 查找可执行文件
        /// </summary>
        /// <param name="executableName">可执行文件名称</param>
        /// <returns>可执行文件的信息</returns>
        public static FileInfo? FindExecutable(string executableName)
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            var paths = path.Split0(Path.PathSeparator);

            foreach (var suffix in ExecutableSuffixes)
            {
                var pathToExe = executableName + suffix;
                if (File.Exists(pathToExe))
                    return new(pathToExe);
            }

            foreach (var p in paths)
            {
                foreach (var suffix in ExecutableSuffixes)
                {
                    var pathToExe = Path.Combine(p, executableName + suffix);
                    if (File.Exists(pathToExe))
                        return new(pathToExe);
                }
            }

            return null;
        }
    }
}
