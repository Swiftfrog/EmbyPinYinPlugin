// PinyinProviderHelper.cs
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using PinYinSort.Utils;
using System;
using System.Linq;

namespace PinyinSeek.Utils;

/// <summary>
/// 提供 PinYinSort 插件中元数据处理的通用辅助方法。
/// 可被 ICustomMetadataProvider 和 IScheduledTask 共享使用。
/// </summary>
public static class PinyinProviderHelper
{
    /// <summary>
    /// 判断是否应更新项目的 SortName 字段。
    /// </summary>
    public static bool ShouldUpdateSortName(BaseItem item, string expectedPinyin, PinyinSeekConfig config)
    {
        var current = item.SortName;

        // 1. 如果排序功能未启用，不更新
        if (!config.EnablePinyinSort)
            return false;

        // 2. 如果字段被锁定，插件全权负责：仅当值不匹配时更新
        if (item.LockedFields?.Contains(MetadataFields.SortName) == true)
        {
            return !string.Equals(current, expectedPinyin, StringComparison.Ordinal);
        }

        // 3. 如果当前 SortName 包含中文，必须更新（不适合排序）
        if (!string.IsNullOrEmpty(current) && PinyinHelper.ContainsChinese(current))
            return true;

        // 4. 如果开启“仅当为空时填充”且当前非空（且不含中文），跳过
        if (config.OnlyFillWhenEmpty && !string.IsNullOrEmpty(current))
            return false;

        // 5. 其他情况：为空 或 未开启保守模式 → 允许更新
        return string.IsNullOrEmpty(current);
    }

    /// <summary>
    /// 安全地将字段添加到 LockedFields，避免覆盖已有锁定项。
    /// </summary>
    public static void SafeAddLockedField(BaseItem item, MetadataFields field)
    {
        var current = item.LockedFields ?? System.Array.Empty<MetadataFields>();
        if (!current.Contains(field))
        {
            item.LockedFields = current.Concat(new[] { field }).ToArray();
        }
    }
}
