// PinYinSortConfig.cs
using MediaBrowser.Model.Plugins;
using Emby.Web.GenericEdit;
using System.ComponentModel;

namespace PinyinSeek;

public class PinyinSeekConfig : EditableOptionsBase
{
    public override string EditorTitle => "PinyinSeek Settings";

    /// <summary>
    /// 是否启用拼音排序（设置 SortName）
    /// </summary>
    [DisplayName("启用拼音排序")]
    [Description("是否启用拼音排序功能")]
    public bool EnablePinyinSort { get; set; } = true;

    /// <summary>
    /// 是否启用拼音搜索（在 OriginalTitle 末尾添加 #拼音）
    /// </summary>
    [DisplayName("启用拼音搜索")]
    [Description("是否启用拼音搜索功能。OriginalTitle 末尾会被添加 #拼音")]
    public bool EnablePinyinSearch { get; set; } = true;

    /// <summary>
    /// 是否启用计划任务（批量处理媒体库-拼音排序&搜索）
    /// </summary>
    [DisplayName("启用拼音排序或搜索任务")]
    [Description("媒体库执行拼音排序或搜索任务，全库扫描修改。默认关闭。")]
    public bool EnableScheduledTask { get; set; } = false; // 默认关闭，避免意外全库扫描

    /// <summary>
    /// 仅在 SortName 为空时填充拼音（避免覆盖用户自定义排序）
    /// </summary>
    [DisplayName("启用自定义排序")]
    [Description("打开情况下，自定义排序不会被修订。如果自定义排序是中文，依旧会被拼音化。不理解，请关闭。")]
    public bool OnlyFillWhenEmpty { get; set; } = false;
    
}
