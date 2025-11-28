// PinyinSeekConfig.cs
using MediaBrowser.Model.Plugins;
using Emby.Web.GenericEdit;
using System.ComponentModel;

namespace PinyinSeek;

public class PinyinSeekConfig : EditableOptionsBase
{
    public override string EditorTitle => "PinyinSeek Settings";

    /// 是否启用拼音排序（设置 SortName）
    [DisplayName("启用拼音排序")]
    [Description("是否启用拼音排序功能")]
    public bool EnablePinyinSort { get; set; } = true;

    /// 是否启用拼音搜索（在 OriginalTitle 末尾添加 #拼音）
    [DisplayName("启用拼音搜索")]
    [Description("是否启用拼音搜索功能。OriginalTitle 末尾会被添加 #拼音")]
    public bool EnablePinyinSearch { get; set; } = true;

    [DisplayName("启用国家标签")]
    [Description("启用后，标签中添加 国家 (代码)")]
    public bool EnableCountryAsTag { get; set; } = false;

    [DisplayName("启用 IMDb Top 标签")]
    [Description("启用后，标签中添加 IMDb Top 250")]
    public bool EnableImdbTopTag { get; set; } = false;

    /// 是否启用计划任务（批量处理媒体库-拼音排序&搜索&标签）
    [DisplayName("启用插件计划任务")]
    [Description("是否启用拼音排序/搜索/标签计划任务，全库扫描修改。默认关闭。")]
    public bool EnableScheduledTask { get; set; } = false;

    /// 仅在 SortName 为空时填充拼音（避免覆盖用户自定义排序）
    [DisplayName("启用自定义排序")]
    [Description("打开情况下，自定义排序不会被修订。如果自定义排序是中文，依旧会被拼音化。不理解，请关闭。")]
    public bool OnlyFillWhenEmpty { get; set; } = false;
    
}