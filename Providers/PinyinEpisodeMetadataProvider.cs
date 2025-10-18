// Providers/PinyinSeriesMetadataProvider.cs
using MediaBrowser.Controller.Entities.TV; // Series
using Microsoft.Extensions.Logging;

namespace EmbyPinyinPlugin.Providers
{
    public class PinyinSeriesMetadataProvider : BasePinyinMetadataProvider<Series>
    {
        public PinyinSeriesMetadataProvider(ILogger<PinyinSeriesMetadataProvider> logger) : base(logger) { }

        public override string Name => "Pinyin Series Sorter";
    }
}

// Providers/PinyinEpisodeMetadataProvider.cs
using MediaBrowser.Controller.Entities.TV; // Episode
using Microsoft.Extensions.Logging;

namespace EmbyPinyinPlugin.Providers
{
    public class PinyinEpisodeMetadataProvider : BasePinyinMetadataProvider<Episode>
    {
        public PinyinEpisodeMetadataProvider(ILogger<PinyinEpisodeMetadataProvider> logger) : base(logger) { }

        public override string Name => "Pinyin Episode Sorter";
    }
}

// ... 为其他类型创建类似的提供者 ...
