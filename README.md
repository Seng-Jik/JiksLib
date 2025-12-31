# JiksLib

作为 Unity 工程师 工作时积累下的用于 Unity 游戏开发的工具库。

## 安装

### 用于 Unity

将 `https://github.com/Seng-Jik/JiksLib.git?path=Assets/JiksLib` 添加到 Unity 包中。

### 用于 .NET

在 NuGet Gallery 中查找 `JiksLib.Core` 后添加到你的项目中，只包含不依赖 Unity 部分的功能。

## JiksLib.Core

这个库可以脱离 Unity 单独使用。

### 测试

可以对 `JiksLib.Core.Test` 项目执行 `dotnet test` （需要 .NET SDK 10 或以上版本）以测试该项目。

## JiksLib.UniTask

这个库为 JiksLib 添加了 UniTask 相关支持，添加 `JIKS_LIB_ENABLE_UNITASK` 编译符号并添加 `JiksLib.UniTask.asmdef` 的引用来启用它。
