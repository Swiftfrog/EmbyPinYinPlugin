// Providers/PinyinMovieMetadataProvider.cs
using MediaBrowser.Controller.Entities.Movies; // Movie
using Microsoft.Extensions.Logging;

namespace EmbyPinyinPlugin.Providers
{
    public class PinyinMovieMetadataProvider : BasePinyinMetadataProvider<Movie>
    {
        public PinyinMovieMetadataProvider(ILogger<PinyinMovieMetadataProvider> logger) : base(logger) { }

        public override string Name => "Pinyin Movie Sorter";
    }
}
