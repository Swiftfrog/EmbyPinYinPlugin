// Utils/CountryTagHelper.cs
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Logging;
using System.Linq;
using System;
using PinyinSeek.Utils;

namespace PinyinSeek.Utils;

/// <summary>
/// 提供国家标签处理的通用方法，可被 Provider 和 ScheduledTask 共享。
/// </summary>
public static class CountryTagHelper
{
    /// <summary>
    /// 尝试为项目添加国家标签。
    /// </summary>
    /// <param name="item">媒体项目</param>
    /// <param name="config">插件配置</param>
    /// <param name="logger">日志记录器（可选，用于详细日志）</param>
    /// <param name="typeName">调用方类型名称（用于日志）</param>
    /// <returns>是否添加了新标签</returns>
    public static bool TryAddCountryTag(BaseItem item, PinyinSeekConfig config, ILogger logger = null, string typeName = null)
    {
        if (!config.EnableCountryAsTag)
            return false;

        var originalCountry = item.ProductionLocations?.FirstOrDefault();
        if (string.IsNullOrEmpty(originalCountry))
        {
            logger?.Debug($"[PinyinSeek] {typeName ?? "CountryTag"}: 项目无原产国家信息。ID: {item.Id}");
            return false;
        }

        var displayCountry = CountryMapper.GetLocalizedCountry(originalCountry);
        
        // 避免重复添加
        if (item.Tags?.Contains(displayCountry, StringComparer.OrdinalIgnoreCase) == true)
        {
            logger?.Debug($"[PinyinSeek] {typeName ?? "CountryTag"}: 项目已包含国家标签 \"{displayCountry}\"。ID: {item.Id}");
            return false;
        }

        // 添加国家标签
        var newTags = (item.Tags ?? System.Array.Empty<string>()).ToList();
        newTags.Add(displayCountry);
        item.Tags = newTags.ToArray();
        
        logger?.Debug($"[PinyinSeek] {typeName ?? "CountryTag"}: 已添加国家标签 \"{displayCountry}\"（原值: {originalCountry}）。ID: {item.Id}");
        return true;
    }
}