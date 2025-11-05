// Plugin.cs
using MediaBrowser.Common; // 引入你的提供者和任务命名空间
using MediaBrowser.Common.Configuration; // IConfigurationManager
using MediaBrowser.Common.Plugins; // IPlugin, BasePlugin
using MediaBrowser.Model.Drawing; // 
using MediaBrowser.Model.Plugins; // BasePluginConfiguration
using MediaBrowser.Model.Serialization; // IJsonSerializer
using System;// IApplicationHost 在 MediaBrowser.Common 命名空间下
using System.IO;

using PinYinSort.Providers;

namespace PinYinSort;

public class Plugin : BasePlugin<PluginConfiguration>, IPlugin, IHasThumbImage
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths, xmlSerializer)
    {
    }

    public override string Name => "PinyinSorter";

    public override Guid Id => Guid.Parse("B250C7F4-4E2B-4E7C-8B9A-123456789ABC"); // 请替换成你生成的全新 GUID

    public override string Description => "Adds pinyin initials to media items for sorting and searching.";
    
    // 添加这个 ThumbImage 属性
    public Stream GetThumbImage()
    {
        var assembly = GetType().Assembly;
        string resourceName = "PinYinSortLogo.webp";
        return assembly.GetManifestResourceStream(resourceName);
    }
    public ImageFormat ThumbImageFormat => ImageFormat.Webp;
    
    
}

public class PluginConfiguration : BasePluginConfiguration
{
    // 可以在这里添加插件配置选项，例如是否处理 OriginalTitle 等
    // public bool ProcessOriginalTitle { get; set; } = false;
}