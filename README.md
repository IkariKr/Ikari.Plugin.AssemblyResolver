# Generic Assembly Resolver for .NET Plugins

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**一行代码，告别 .NET 插件开发中的"DLL地狱"！**

您是否厌倦了因为 `Newtonsoft.Json`, `RestSharp` 或其他任何共享库的版本不同，而导致您的插件（Add-in）在客户端崩溃？您是否受够了 `System.IO.FileNotFoundException` 或 `Could not load file or assembly` 这样的运行时错误？

`GenericAssemblyResolver` 是一个轻量级、零依赖的通用库，它提供了一个极其简单且健壮的解决方案，来应对在共享应用程序域（Shared AppDomain）中运行的.NET插件所面临的程序集加载冲突问题。

## 痛点：为什么需要它？ (The Problem)

像 AutoCAD, Revit, SolidWorks, VSTO for Office 等许多大型桌面软件，都采用一种插件架构：将所有第三方插件（DLLs）加载到同一个.NET应用程序域（AppDomain）中。这导致了一个经典问题——"DLL地狱"：

*   **版本冲突：** 插件A依赖 `Newtonsoft.Json v12.0`，插件B依赖 `v13.0`。哪个插件先被加载，它的依赖版本就会被锁定在内存中，导致后一个插件因版本不匹配而崩溃。
*   **加载路径问题：** .NET运行时可能找不到您插件目录下的特定DLL，尤其是在复杂的宿主环境中。
*   **无法修改主机配置：** 解决方案（如修改 `acad.exe.config`）通常要求修改最终用户的电脑，这在大多数情况下是不可行的。

## 解决方案 (The Solution)

本库提供了一个 `GenericAssemblyResolver` 类，它通过一种自包含的方式优雅地解决了上述所有问题。

它通过订阅 `AppDomain.CurrentDomain.AssemblyResolve` 事件，智能地拦截失败的程序集加载请求。当.NET运行时找不到一个您的插件所需的DLL时，这个解析器会介入，并**强制运行时从您插件自己的目录下加载正确的DLL文件**。

这一切都是自动的，并且完全隔离在您的插件内部，不会影响任何其他插件。

## ✨ 特性 (Features)

*   **一劳永逸解决"DLL地狱"**：彻底告别因共享依赖版本冲突引发的运行时错误。
*   **零配置**：无需修改任何 `.config` 文件，对最终用户透明。
*   **自包含**：所有逻辑都在您的插件内部，不干扰宿主或其他插件。
*   **轻量且零依赖**：库本身极小，且不依赖任何第三方包。
*   **超广的兼容性**：通过多目标框架，同时支持从 **.NET Framework 3.5** 到最新的 **.NET 8** 及更高版本。
*   **极易集成**：只需在您的插件入口处实例化一个类即可。

## 🚀 快速开始 (Getting Started)

只需两步，即可在您的插件项目中启用它。

### 1. 安装 NuGet 包

通过 NuGet 包管理器控制台安装：
```c
Install-Package YourCompany.Plugin.AssemblyResolver
```
或者通过 .NET CLI：
```
dotnet add package YourCompany.Plugin.AssemblyResolver
```

### 2. 在插件入口处初始化解析器

在您插件的入口类（例如，实现 IExtensionApplication for AutoCAD, IExternalApplication for Revit 的类）中，创建并管理 `GenericAssemblyResolver` 的实例。
```csharp
// 1. 引入命名空间
using YourCompany.Plugin.AssemblyResolver; // 替换为您自己的命名空间
using Autodesk.AutoCAD.Runtime; // 这是一个示例，适用于任何插件框架
using System.Reflection;

public class MyPluginEntryPoint : IExtensionApplication // 示例入口
{
    // 2. 声明一个字段来持有解析器的实例
    private GenericAssemblyResolver _assemblyResolver;

    public void Initialize()
    {
        // 3. 在插件初始化时，创建解析器的实例
        //    这是最关键的一步：必须将当前插件的程序集信息传递给它，
        //    这样它才知道去哪里寻找依赖的DLL文件。
        _assemblyResolver = new GenericAssemblyResolver(Assembly.GetExecutingAssembly());

        // --- 您其他的插件初始化代码放在这里 ---
    }

    public void Terminate()
    {
        // 4. 在插件终止时，销毁实例以取消事件订阅
        _assemblyResolver?.Dispose();
    }
}
```
**完成了！** 现在，您的插件将能够可靠地加载它自带的所有依赖项，无论宿主环境多么复杂。

## 兼容性 (Compatibility)

本库通过多目标编译，支持以下.NET平台：

* .NET Framework 3.5
* .NET Framework 4.6.1+
* .NET Standard 2.0
    * (.NET Core 2.0+)
    * (.NET 5/6/7/8+)

## 如何贡献 (Contributing)

欢迎提交 Issues 和 Pull Requests！如果您发现了bug或有任何改进建议，请随时提出。

## 授权许可 (License)

本项目采用 MIT License 授权。