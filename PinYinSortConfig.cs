// PinYinSortConfig.cs
using MediaBrowser.Model.Plugins;
using Emby.Web.GenericEdit;

namespace PinYinSort;

public class PinYinSortConfig : EditableOptionsBase
{
    public override string EditorTitle => "PinYinSort Settings";

    /// <summary>
    /// 是否启用拼音排序（设置 SortName）
    /// </summary>
    public bool EnablePinyinSort { get; set; } = true;

    /// <summary>
    /// 是否启用拼音搜索（在 OriginalTitle 末尾添加 #拼音）
    /// </summary>
    public bool EnablePinyinSearch { get; set; } = true;

    /// <summary>
    /// 是否启用计划任务（批量处理媒体库-拼音排序&搜索）
    /// </summary>
    public bool EnableScheduledTask { get; set; } = false; // 默认关闭，避免意外全库扫描

    /// <summary>
    /// 仅在 SortName 为空时填充拼音（避免覆盖用户自定义排序）
    /// </summary>
    public bool OnlyFillWhenEmpty { get; set; } = true;
    
}