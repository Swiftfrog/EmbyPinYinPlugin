// PinYinSortConfig.cs
using MediaBrowser.Model.Plugins;
using Emby.Web.GenericEdit;

namespace PinYinSort;

public class PinYinSortConfig : EditableOptionsBase
{
    public override string EditorTitle => "PinYinSort Settings";
 
    /// <summary>
    /// 是否处理 OriginalTitle 字段（默认 true）
    /// </summary>
    public bool ProcessOriginalTitle { get; set; } = true;

    /// <summary>
    /// 是否启用详细日志（用于调试）
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// 是否仅在无排序名称时生成拼音首字母（避免覆盖用户自定义排序）
    /// </summary>
    public bool OnlyFillWhenEmpty { get; set; } = true;
}