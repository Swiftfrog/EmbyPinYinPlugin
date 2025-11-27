// Utils/CountryMapper.cs
using System.Collections.Generic;

namespace PinyinSeek.Utils;

public static class CountryMapper
{
    private static readonly Dictionary<string, string> _countryMap = new Dictionary<string, string>
    {
        // 常见国家/地区映射（英文 -> 中文）
        { "United States of America", "美国" },
        { "United States", "美国" },
        { "USA", "美国" },
        { "China", "中国" },
        { "Hong Kong", "中国香港" },
        { "Taiwan", "中国台湾" },
        { "Japan", "日本" },
        { "South Korea", "韩国" },
        { "United Kingdom", "英国" },
        { "Germany", "德国" },
        { "France", "法国" },
        { "Italy", "意大利" },
        { "Spain", "西班牙" },
        { "Russia", "俄罗斯" },
        { "India", "印度" },
        { "Canada", "加拿大" },
        { "Australia", "澳大利亚" },
        { "Brazil", "巴西" },
        { "Mexico", "墨西哥" },
        // 可根据需要继续扩展
    };

    public static string GetLocalizedCountry(string originalCountry)
    {
        if (string.IsNullOrEmpty(originalCountry))
            return originalCountry;

        // 优先尝试精确匹配
        if (_countryMap.TryGetValue(originalCountry, out var localized))
            return localized;

        // 如果没匹配上，返回原始值（避免丢失信息）
        return originalCountry;
    }
}
