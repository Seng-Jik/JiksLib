# JiksLib

作为 Unity 工程师 工作时积累下的用于 Unity 游戏开发的工具库。

## JiksLib.Core

这个库可以脱离 Unity 单独使用，也可以在 Unity 中使用。

### 测试

可以对 `JiksLib.Core.Test` 项目执行 `dotnet test` （需要 .NET SDK 10 或以上版本）以测试该项目。

### 安装

#### 用于 Unity

将 `https://github.com/Seng-Jik/JiksLib.git?path=Assets/JiksLib/JiksLib.Core` 添加到 Unity 包中。

#### 用于 .NET

在 NuGet Gallery 中查找 `JiksLib.Core` 后添加到你的项目中。

### UniTask 支持

如果在 Unity 中安装了 UniTask，则设置 `JIKS_LIB_ENABLE_UNITASK` 编译符号来启用 UniTask支持。
