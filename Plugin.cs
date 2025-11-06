// Plugin.cs
using MediaBrowser.Common; // 引入你的提供者和任务命名空间
using MediaBrowser.Common.Plugins; // IPlugin, BasePlugin
using MediaBrowser.Controller.Plugins; // BasePluginSimpleUI
using MediaBrowser.Model.Drawing; // ImageFormat
using MediaBrowser.Model.Plugins; // 
using System;
using System.IO;

namespace PinYinSort;

public class Plugin : BasePluginSimpleUI<PinYinSortConfig>, IHasThumbImage
{
    public override Guid Id => new Guid("B250C7F4-4E2B-4E7C-8B9A-123456789ABC");
    public override string Name => "PinyinSorter";
    public override string Description => "Adds pinyin initials to media items for sorting and searching.";
        
    // 静态实例，便于在 Provider 等类中访问配置
    public static Plugin Instance { get; private set; } = null!;

    // 构造函数：BasePluginSimpleUI 要求传入 IApplicationHost
    public Plugin(IApplicationHost applicationHost) : base(applicationHost)
    {
        Instance = this;
    }

    // 实现 IHasThumbImage
    public Stream GetThumbImage()
    {
        var assembly = GetType().Assembly;
        return assembly.GetManifestResourceStream("PinYinSort.PinYinSortLogo.webp");
    }

    public ImageFormat ThumbImageFormat => ImageFormat.Webp;

    /// <summary>
    /// 当用户在 UI 中保存配置后触发
    /// 可用于重新加载逻辑、通知服务等
    /// </summary>
    protected override void OnOptionsSaved(PinYinSortConfig options)
    {
        // 例如：记录日志、触发缓存刷新等
        // 注意：此处 options 已保存到磁盘
        base.OnOptionsSaved(options);
    }

    // /// <summary>
    // /// （可选）在保存前验证或取消保存
    // /// 返回 false 可阻止保存
    // /// </summary>
    // protected override bool OnOptionsSaving(PinYinSortConfig options)
    // {
    //     // 例如：验证字段合法性
    //     return base.OnOptionsSaving(options);
    // }
}
