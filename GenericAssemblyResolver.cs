using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Ikari.Plugin.AssemblyResolver;


/// <summary>
/// 一个通用的程序集解析器，用于解决AutoCAD插件的“DLL地狱”问题。
/// </summary>
public sealed class GenericAssemblyResolver : IDisposable
{
    private readonly string _pluginDirectory;
    private readonly HashSet<string> _resolvableAssemblies;

    /// <summary>
    /// 初始化解析器。
    /// </summary>
    /// <param name="pluginAssembly">调用此解析器的插件程序集。必须传入，以便解析器能正确定位插件的目录。</param>
    public GenericAssemblyResolver(Assembly? pluginAssembly)
    {
        if (pluginAssembly == null)
            throw new ArgumentNullException(nameof(pluginAssembly));
        
        _pluginDirectory = Path.GetDirectoryName(pluginAssembly.Location);
        
        _resolvableAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        PreScanDirectory();
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    /// <summary>
    /// 预扫描插件目录，将所有.dll文件的程序集简单名称添加到缓存中。
    /// </summary>
    private void PreScanDirectory()
    {
        if (!Directory.Exists(_pluginDirectory)) return;

        // 遍历目录下的所有DLL文件
        foreach (var file in Directory.GetFiles(_pluginDirectory, "*.dll"))
        {
            try
            {
                var assemblyName = AssemblyName.GetAssemblyName(file).Name;
                _resolvableAssemblies.Add(assemblyName);
            }
            catch (BadImageFormatException)
            {
                // 忽略非.NET程序集的DLL文件
            }
            catch (Exception)
            {
                // 忽略其他可能的错误
            }
        }
    }

    /// <summary>
    /// 当.NET运行时无法找到程序集时，此方法会被调用。
    /// </summary>
    private Assembly? OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
        // 1. 获取请求的程序集的简单名称
        var requestedAssemblyName = new AssemblyName(args.Name).Name;

        // 2. 检查这个程序集是否在我们预扫描的列表中
        if (!_resolvableAssemblies.Contains(requestedAssemblyName))
        {
            // 如果不在，说明它不是我们插件目录下的依赖项，返回null让.NET继续默认查找
            return null;
        }

        // 3. 构造我们插件目录下的目标DLL的完整路径
        var targetDllPath = Path.Combine(_pluginDirectory, requestedAssemblyName + ".dll");

        // 4. 从该路径加载并返回程序集
        if (!File.Exists(targetDllPath)) return null;
        try
        {
            // 使用LoadFrom从指定路径加载，这是处理此类问题的标准方法
            return Assembly.LoadFrom(targetDllPath);
        }
        catch
        {
            // 如果加载失败，返回null
            return null;
        }
        
    }

    /// <summary>
    /// 实现IDisposable接口，用于在插件卸载时取消事件订阅，防止内存泄漏。
    /// </summary>
    public void Dispose()
    {
        AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
    }
}
