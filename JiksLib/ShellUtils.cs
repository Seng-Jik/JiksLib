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
        /// Windows 下由 PATHEXT 环境变量决定，其他平台为 { "", ".sh" }
        /// </summary>
        public readonly static IReadOnlyList<string> ExecutableSuffixes =
            GetExecutableSuffixes();

        /// <summary>
        /// 查找可执行文件
        /// 显式路径（绝对路径或含目录分隔符）直接检查该路径，
        /// 其余情况只在 PATH 中查找，不查找当前工作目录
        /// </summary>
        /// <param name="executableName">可执行文件名称</param>
        /// <returns>可执行文件的信息</returns>
        public static FileInfo? FindExecutable(string executableName)
        {
            executableName.ThrowIfNull();

            var candidates = GetNameCandidates(executableName);

            if (IsExplicitPath(executableName))
            {
                // 显式路径先按原样检查：无扩展名的绝对路径不应被后缀规则改写
                if (IsExecutable(executableName))
                    return new(executableName);

                foreach (var candidate in candidates)
                {
                    if (IsExecutable(candidate))
                        return new(candidate);
                }

                return null;
            }

            var path = Environment.GetEnvironmentVariable("PATH") ?? "";

            foreach (var entry in path.Split0(Path.PathSeparator))
            {
                var directory = NormalizePathEntry(entry);

                if (directory.Length == 0)
                    continue;

                foreach (var candidate in candidates)
                {
                    var pathToExe = Path.Combine(directory, candidate);

                    if (IsExecutable(pathToExe))
                        return new(pathToExe);
                }
            }

            return null;
        }

        /// <summary>
        /// 得到待检查的文件名列表
        /// 已带扩展名时只检查原名称，否则依次追加可执行文件后缀
        /// </summary>
        static string[] GetNameCandidates(string executableName)
        {
            if (Path.HasExtension(executableName))
                return new string[] { executableName };

            var suffixes = ExecutableSuffixes;
            var candidates = new string[suffixes.Count];

            for (var i = 0; i < suffixes.Count; i++)
                candidates[i] = executableName + suffixes[i];

            return candidates;
        }

        /// <summary>
        /// 路径是否为显式路径（绝对路径或含目录分隔符）
        /// </summary>
        static bool IsExplicitPath(string path) =>
            Path.IsPathRooted(path) ||
            path.IndexOf(Path.DirectorySeparatorChar) >= 0 ||
            path.IndexOf(Path.AltDirectorySeparatorChar) >= 0;

        /// <summary>
        /// 去除 PATH 条目的空白与引号
        /// </summary>
        static string NormalizePathEntry(string entry)
        {
            var path = entry.Trim();

            if (path.Length >= 2 && path[0] == '"' && path[path.Length - 1] == '"')
                path = path.Substring(1, path.Length - 2).Trim();

            return path;
        }

        /// <summary>
        /// 判断给定路径是否为可执行文件
        /// </summary>
        static bool IsExecutable(string path)
        {
            if (!File.Exists(path))
                return false;

#if NET6_0_OR_GREATER
            if (OperatingSystem.IsWindows())
                return true;

            try
            {
                const UnixFileMode executeMode =
                    UnixFileMode.UserExecute |
                    UnixFileMode.GroupExecute |
                    UnixFileMode.OtherExecute;

                return (File.GetUnixFileMode(path) & executeMode) != 0;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
#else
            return true;
#endif
        }

        /// <summary>
        /// 得到当前平台下的可执行文件后缀
        /// Windows 下解析 PATHEXT，为空时回退到 .com;.exe;.bat;.cmd
        /// </summary>
        static IReadOnlyList<string> GetExecutableSuffixes()
        {
            if (!IsCurrentOSWindowsFamily)
                return new string[] { "", ".sh" };

            var suffixes = new List<string>();
            var knownSuffixes = new HashSet<string>(StringComparer.Ordinal);
            var pathExt = Environment.GetEnvironmentVariable("PATHEXT");

            if (!string.IsNullOrEmpty(pathExt))
            {
                foreach (var item in pathExt.Split(';'))
                {
                    var suffix = item.Trim().ToLowerInvariant();

                    if (suffix.Length == 0 || suffix[0] != '.')
                        continue;

                    if (knownSuffixes.Add(suffix))
                        suffixes.Add(suffix);
                }
            }

            if (suffixes.Count == 0)
            {
                suffixes.Add(".com");
                suffixes.Add(".exe");
                suffixes.Add(".bat");
                suffixes.Add(".cmd");
            }

            // 部分系统的 PATHEXT 不含 PowerShell 脚本；本库历史上始终把 .ps1
            // 视为可执行后缀，这里补上以保持既有契约。
            if (!knownSuffixes.Contains(".ps1") && !suffixes.Contains(".ps1"))
                suffixes.Add(".ps1");

            return suffixes;
        }
    }
}
