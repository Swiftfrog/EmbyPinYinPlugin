// Utils/CountryMapper.cs
using System.Collections.Generic;
using System;

namespace PinyinSeek.Utils;

/// <summary>
/// 提供 ISO 3166-1 两位字母国家代码到中文国家名称的映射。
/// 用于将 TMDb origin_country（如 "US"）转换为用户友好的中文标签（如 "美国"）。
/// </summary>
public static class CountryMapper
{
    private static readonly Dictionary<string, string> _countryMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Americas
        { "US", "美国" },
        { "CA", "加拿大" },
        { "MX", "墨西哥" },
        { "BR", "巴西" },
        { "AR", "阿根廷" },
        { "CL", "智利" },
        { "CO", "哥伦比亚" },
        { "PE", "秘鲁" },
        { "VE", "委内瑞拉" },
        { "CU", "古巴" },

        // Europe
        { "GB", "英国" },
        { "FR", "法国" },
        { "DE", "德国" },
        { "IT", "意大利" },
        { "ES", "西班牙" },
        { "RU", "俄罗斯" },
        { "NL", "荷兰" },
        { "SE", "瑞典" },
        { "NO", "挪威" },
        { "DK", "丹麦" },
        { "FI", "芬兰" },
        { "PL", "波兰" },
        { "GR", "希腊" },
        { "PT", "葡萄牙" },
        { "IE", "爱尔兰" },
        { "CH", "瑞士" },
        { "AT", "奥地利" },
        { "BE", "比利时" },
        { "CZ", "捷克" },
        { "HU", "匈牙利" },
        { "RO", "罗马尼亚" },
        { "BG", "保加利亚" },
        { "HR", "克罗地亚" },
        { "RS", "塞尔维亚" },
        { "UA", "乌克兰" },
        { "EE", "爱沙尼亚" },
        { "LV", "拉脱维亚" },
        { "LT", "立陶宛" },
        { "SI", "斯洛文尼亚" },
        { "SK", "斯洛伐克" },
        { "IS", "冰岛" },
        { "LU", "卢森堡" },
        { "MT", "马耳他" },
        { "CY", "塞浦路斯" },

        // Asia
        { "CN", "中国" },
        { "JP", "日本" },
        { "KR", "韩国" },
        { "IN", "印度" },
        { "ID", "印度尼西亚" },
        { "TH", "泰国" },
        { "VN", "越南" },
        { "MY", "马来西亚" },
        { "SG", "新加坡" },
        { "PH", "菲律宾" },
        { "PK", "巴基斯坦" },
        { "BD", "孟加拉国" },
        { "LK", "斯里兰卡" },
        { "MM", "缅甸" },
        { "KH", "柬埔寨" },
        { "LA", "老挝" },
        { "MN", "蒙古" },
        { "IL", "以色列" },
        { "SA", "沙特阿拉伯" },
        { "AE", "阿联酋" },
        { "IR", "伊朗" },
        { "TR", "土耳其" },
        { "KZ", "哈萨克斯坦" },
        { "UZ", "乌兹别克斯坦" },
        { "QA", "卡塔尔" },
        { "KW", "科威特" },
        { "BH", "巴林" },
        { "OM", "阿曼" },
        { "YE", "也门" },
        { "IQ", "伊拉克" },
        { "SY", "叙利亚" },
        { "JO", "约旦" },
        { "LB", "黎巴嫩" },
        { "GE", "格鲁吉亚" },
        { "AM", "亚美尼亚" },
        { "AZ", "阿塞拜疆" },

        // Oceania
        { "AU", "澳大利亚" },
        { "NZ", "新西兰" },
        { "FJ", "斐济" },
        { "PG", "巴布亚新几内亚" },

        // Africa
        { "ZA", "南非" },
        { "EG", "埃及" },
        { "NG", "尼日利亚" },
        { "KE", "肯尼亚" },
        { "MA", "摩洛哥" },
        { "DZ", "阿尔及利亚" },
        { "TN", "突尼斯" },
        { "GH", "加纳" },
        { "TZ", "坦桑尼亚" },
        { "UG", "乌干达" },
        { "ZW", "津巴布韦" },
        { "SN", "塞内加尔" },
        { "CM", "喀麦隆" },
        { "CI", "科特迪瓦" },
        { "ML", "马里" },
        { "ET", "埃塞俄比亚" },
        { "SD", "苏丹" },

        // Special Administrative Regions (common in media context)
        { "HK", "中国香港" },
        { "MO", "中国澳门" },
        { "TW", "中国台湾" }
    };

    /// <summary>
    /// 根据 ISO 3166-1 两位字母国家代码返回对应的中文国家名称。
    /// </summary>
    /// <param name="countryCode">例如 "US", "GB", "CN"</param>
    /// <returns>中文国家名，如 "美国"；若未知则返回 null</returns>
    public static string GetLocalizedCountry(string countryCode)
    {
        if (string.IsNullOrEmpty(countryCode))
            return null;

        return _countryMap.TryGetValue(countryCode, out var name) ? name : null;
    }
}