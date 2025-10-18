// Providers/PinyinMovieMetadataProvider.cs
using MediaBrowser.Controller.Entities.Movies; // Movie
// 移除 Microsoft.Extensions.Logging 的 using
// using Microsoft.Extensions.Logging; // ILogger<T>

namespace EmbyPinyinPlugin.Providers
{
    public class PinyinMovieMetadataProvider : BasePinyinMetadataProvider<Movie>
    {
        // 移除 ILogger 参数的构造函数
        // public PinyinMovieMetadataProvider(ILogger<PinyinMovieMetadataProvider> logger) : base(logger) { }

        // 无参数构造函数
        public PinyinMovieMetadataProvider() : base() { }

        public override string Name => "Pinyin Movie Sorter";
    }
}
