// Utils/PinyinHelper.cs
using TinyPinyin;
//using System.Text.RegularExpressions; // 如果使用正则表达式

namespace EmbyPinyinPlugin.Utils
{
    public static class PinyinHelper
    {
        /// <summary>
        /// 将中文文本转换为拼音首字母。
        /// </summary>
        /// <param name="chineseText">输入的中文文本。</param>
        /// <returns>拼音首字母组成的字符串，例如 "独立日" -> "dlr"。如果输入为空，则返回空字符串。</returns>
        public static string GetPinyinInitials(string chineseText)
        {
            if (string.IsNullOrWhiteSpace(chineseText))
                return string.Empty;

            // 使用 TinyPinyin 的 GetPinyinInitials 方法
            // separator 为 "" 表示不分割，直接连接所有首字母
            return TinyPinyin.PinyinHelper.GetPinyinInitials(chineseText, "");
        }

        /// <summary>
        /// 判断字符串是否包含中文字符。
        /// </summary>
        /// <param name="text">待判断的字符串。</param>
        /// <returns>如果包含至少一个中文字符，则返回 true；否则返回 false。</returns>
        public static bool ContainsChinese(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            // 方法一：使用 TinyPinyin 库的 IsChinese 方法
            foreach (char c in text)
            {
                if (TinyPinyin.PinyinHelper.IsChinese(c))
                {
                    return true;
                }
            }
            return false;

            // 方法二：使用 Unicode 范围判断 (可选，作为 TinyPinyin 的备选)
            // return Regex.IsMatch(text, @"[\u4e00-\u9fff]");
        }
    }
}
