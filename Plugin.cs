// Plugin.cs
using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Plugins;
using System;
using System.IO;

namespace PinyinSeek;

public class Plugin : BasePluginSimpleUI<PinyinSeekConfig>, IHasThumbImage
{
    public override string Name => "PinyinSeek";
    public override string Description => "Adds pinyin initials to media items for sorting and searching.";
    public override Guid Id => new Guid("B250C7F4-4E2B-4E7C-8B9A-123456789ABC");
        
    public static Plugin Instance { get; private set; } = null!;
    
    public IApplicationHost ApplicationHost { get; }
    
    public PinyinSeekConfig Configuration => GetOptions();

    public Plugin(IApplicationHost applicationHost) : base(applicationHost)
    {
        Instance = this;
        ApplicationHost = applicationHost;
    }

    // 实现 IHasThumbImage
    public Stream GetThumbImage()
    {
        var assembly = GetType().Assembly;
        return assembly.GetManifestResourceStream("PinyinSeek.PinyinSeekLogo.png");
    }

    public ImageFormat ThumbImageFormat => ImageFormat.Png;

    /// 当用户在 UI 中保存配置后触发
    /// 可用于重新加载逻辑、通知服务等
    protected override void OnOptionsSaved(PinyinSeekConfig options)
    {
        // 例如：记录日志、触发缓存刷新等
        // 注意：此处 options 已保存到磁盘
        base.OnOptionsSaved(options);
    }

    // /// （可选）在保存前验证或取消保存
    // /// 返回 false 可阻止保存
    // protected override bool OnOptionsSaving(PinyinSeekConfig options)
    // {
    //     // 例如：验证字段合法性
    //     return base.OnOptionsSaving(options);
    // }
}
