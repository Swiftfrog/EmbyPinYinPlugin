// Utils/CountryTagHelper.cs
using System;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Logging;
using PinyinSeek.Utils;

namespace PinyinSeek.Utils;

public static class CountryTagHelper
{
    // 语言代码 -> 优先国家英文全称（顺序表示优先级）
    private static readonly System.Collections.Generic.Dictionary<string, string[]> LanguageToPreferredCountries
        = new()
        {
            { "en", new[] { "United States of America", "United Kingdom", "Canada", "Australia", "New Zealand", "Ireland" } },
            { "zh", new[] { "China", "Taiwan", "Hong Kong" } },
            { "zh", new[] { "China", "Hong Kong", "Taiwan" } },
            { "cn", new[] { "Hong Kong", "China", "Taiwan" } }, // 👈 显式支持 TMDb 的 "cn"
            // { "yue", new[] { "Hong Kong", "China" } }
            { "ja", new[] { "Japan" } },
            { "ko", new[] { "South Korea" } },
            { "fr", new[] { "France", "Canada", "Belgium", "Switzerland" } },
            { "de", new[] { "Germany", "Austria", "Switzerland" } },
            { "es", new[] { "Spain", "Mexico", "Argentina", "Colombia" } },
            { "ru", new[] { "Russian Federation" } },
            { "pt", new[] { "Brazil", "Portugal" } },
            { "it", new[] { "Italy" } },
            { "nl", new[] { "Netherlands" } },
            { "sv", new[] { "Sweden" } },
            { "pl", new[] { "Poland" } },
            { "tr", new[] { "Turkey" } },
            { "ar", new[] { "Egypt", "Saudi Arabia", "United Arab Emirates" } }
        };

    public static bool TryAddCountryTag(BaseItem item, PinyinSeekConfig config, ILogger logger, string taskName)
    {
        if (item is not Movie movie)
            return false;

        var locations = movie.ProductionLocations; // 英文全称数组，如 ["India", "United States of America"]
        var lang = movie.OriginalLanguage;         // 如 "en"

        if (locations == null || locations.Length == 0)
            return false;

        string selectedCountry = locations[0]; // 默认第一个

        // 如果有语言信息，尝试提升准确性
        if (!string.IsNullOrEmpty(lang) && LanguageToPreferredCountries.TryGetValue(lang, out var preferred))
        {
            foreach (var candidate in preferred)
            {
                if (locations.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                {
                    selectedCountry = candidate;
                    break;
                }
            }
        }

        // 使用你的 CountryMapper 获取 "中文 (代码)" 格式
        string tagValue = CountryMapper.GetLocalizedCountry(selectedCountry);
        string fullTag = $"国家({tagValue})";

        if (!HasTag(item, fullTag))
        {
            AddTag(item, fullTag);
            logger.Debug($"[{taskName}] 添加国家标签: {item.Name} → {fullTag}");
            return true;
        }

        return false;
    }

    private static bool HasTag(BaseItem item, string tag)
        => item.Tags?.Contains(tag, StringComparer.OrdinalIgnoreCase) == true;

    private static void AddTag(BaseItem item, string tag)
    {
        var tags = (item.Tags ?? Array.Empty<string>()).ToList();
        tags.Add(tag);
        item.Tags = tags.ToArray();
    }
}