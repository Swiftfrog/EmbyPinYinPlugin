using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies; // Movie
using MediaBrowser.Controller.Entities.TV; // Series, Episode
using MediaBrowser.Controller.Entities.Audio; // MusicAlbum, MusicArtist
using MediaBrowser.Controller.Providers; // ICustomMetadataProvider, ItemInfo, IDirectoryService
using MediaBrowser.Controller.Library; // 包含 ItemUpdateType
using MediaBrowser.Model.Configuration; // 包含 LibraryOptions
using System.Threading; // 包含 CancellationToken
using System.Threading.Tasks; // 包含 Task
using MediaBrowser.Model.Entities; // 包含 MetadataFields
using System; // 包含 Activator
using PinYinSort.Utils; // 包含 PinyinHelper

namespace PinYinSort.Providers
{
    /// <summary>
    /// 为电影元数据提供拼音首字母排序功能。
    /// </summary>
    public class PinyinCustomMetadataProviderMovie : ICustomMetadataProvider<Movie>, IHasOrder
    {
        public string Name => "Pinyin Custom Sorter Movie";
        public int Order => 0;

        public async Task<ItemUpdateType> FetchAsync(
            MetadataResult<Movie> itemResult,
            MetadataRefreshOptions options,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken)
        {
            var item = itemResult.Item;
            var nameToProcess = item.Name;
            if (string.IsNullOrEmpty(nameToProcess))
                return ItemUpdateType.None;

            // 在后台线程执行拼音判断和计算（CPU-bound）
            var (hasChinese, pinyinInitials) = await Task.Run(() =>
            {
                if (!PinyinHelper.ContainsChinese(nameToProcess))
                    return (false, (string)null);
                return (true, PinyinHelper.GetPinyinInitials(nameToProcess));
            }, cancellationToken);

            if (!hasChinese || string.IsNullOrEmpty(pinyinInitials))
                return ItemUpdateType.None;

            // 主线程执行轻量属性设置（线程安全）
            var pinyinUpper = pinyinInitials.ToUpper();
            item.SetSortNameDirect(pinyinUpper);
            item.LockedFields = new[] { MetadataFields.SortName };

            var currentOriginalTitle = item.OriginalTitle ?? string.Empty;
            if (!currentOriginalTitle.Contains($" #{pinyinUpper}"))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOriginalTitle)
                    ? pinyinUpper
                    : $"{currentOriginalTitle} #{pinyinUpper}";
            }

            return ItemUpdateType.MetadataEdit;
        }
    }

    /// <summary>
    /// 为电视剧元数据提供拼音首字母排序功能。
    /// </summary>
    public class PinyinCustomMetadataProviderSeries : ICustomMetadataProvider<Series>, IHasOrder
    {
        public string Name => "Pinyin Custom Sorter Series";
        public int Order => 0;

        public async Task<ItemUpdateType> FetchAsync(
            MetadataResult<Series> itemResult,
            MetadataRefreshOptions options,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken)
        {
            var item = itemResult.Item;
            var nameToProcess = item.Name;
            if (string.IsNullOrEmpty(nameToProcess))
                return ItemUpdateType.None;

            var (hasChinese, pinyinInitials) = await Task.Run(() =>
            {
                if (!PinyinHelper.ContainsChinese(nameToProcess))
                    return (false, (string)null);
                return (true, PinyinHelper.GetPinyinInitials(nameToProcess));
            }, cancellationToken);

            if (!hasChinese || string.IsNullOrEmpty(pinyinInitials))
                return ItemUpdateType.None;

            var pinyinUpper = pinyinInitials.ToUpper();
            item.SetSortNameDirect(pinyinUpper);
            item.LockedFields = new[] { MetadataFields.SortName };

            var currentOriginalTitle = item.OriginalTitle ?? string.Empty;
            if (!currentOriginalTitle.Contains($" #{pinyinUpper}"))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOriginalTitle)
                    ? pinyinUpper
                    : $"{currentOriginalTitle} #{pinyinUpper}";
            }

            return ItemUpdateType.MetadataEdit;
        }
    }

    /// <summary>
    /// 为剧集元数据提供拼音首字母排序功能。
    /// </summary>
    public class PinyinCustomMetadataProviderEpisode : ICustomMetadataProvider<Episode>, IHasOrder
    {
        public string Name => "Pinyin Custom Sorter Episode";
        public int Order => 0;

        public async Task<ItemUpdateType> FetchAsync(
            MetadataResult<Episode> itemResult,
            MetadataRefreshOptions options,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken)
        {
            var item = itemResult.Item;
            var nameToProcess = item.Name;
            if (string.IsNullOrEmpty(nameToProcess))
                return ItemUpdateType.None;

            var (hasChinese, pinyinInitials) = await Task.Run(() =>
            {
                if (!PinyinHelper.ContainsChinese(nameToProcess))
                    return (false, (string)null);
                return (true, PinyinHelper.GetPinyinInitials(nameToProcess));
            }, cancellationToken);

            if (!hasChinese || string.IsNullOrEmpty(pinyinInitials))
                return ItemUpdateType.None;

            var pinyinUpper = pinyinInitials.ToUpper();
            item.SetSortNameDirect(pinyinUpper);
            item.LockedFields = new[] { MetadataFields.SortName };

            // 注意：Episode 不修改 OriginalTitle（避免重复标注单集）
            return ItemUpdateType.MetadataEdit;
        }
    }

    /// <summary>
    /// 为合集（BoxSet）元数据提供拼音首字母排序功能。
    /// </summary>
    public class PinyinCustomMetadataProviderBoxSet : ICustomMetadataProvider<BoxSet>, IHasOrder
    {
        public string Name => "Pinyin Custom Sorter BoxSet";
        public int Order => 0;

        public async Task<ItemUpdateType> FetchAsync(
            MetadataResult<BoxSet> itemResult,
            MetadataRefreshOptions options,
            LibraryOptions libraryOptions,
            CancellationToken cancellationToken)
        {
            var item = itemResult.Item;
            var nameToProcess = item.Name;
            if (string.IsNullOrEmpty(nameToProcess))
                return ItemUpdateType.None;

            var (hasChinese, pinyinInitials) = await Task.Run(() =>
            {
                if (!PinyinHelper.ContainsChinese(nameToProcess))
                    return (false, (string)null);
                return (true, PinyinHelper.GetPinyinInitials(nameToProcess));
            }, cancellationToken);

            if (!hasChinese || string.IsNullOrEmpty(pinyinInitials))
                return ItemUpdateType.None;

            var pinyinUpper = pinyinInitials.ToUpper();
            item.SetSortNameDirect(pinyinUpper);
            item.LockedFields = new[] { MetadataFields.SortName };

            var currentOriginalTitle = item.OriginalTitle ?? string.Empty;
            if (!currentOriginalTitle.Contains($" #{pinyinUpper}"))
            {
                item.OriginalTitle = string.IsNullOrEmpty(currentOriginalTitle)
                    ? pinyinUpper
                    : $"{currentOriginalTitle} #{pinyinUpper}";
            }

            return ItemUpdateType.MetadataEdit;
        }
    }
}