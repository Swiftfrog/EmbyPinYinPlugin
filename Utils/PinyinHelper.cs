// Utils/PinyinHelper.cs
using TinyPinyin;

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
            return PinyinHelper.GetPinyinInitials(chineseText, "");
        }
    }
}
